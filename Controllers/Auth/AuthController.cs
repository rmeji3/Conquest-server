using Conquest.Dtos.Auth;
using Conquest.Models.AppUsers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;

namespace Conquest.Controllers.Auth
{

    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(
        UserManager<AppUser> users,
        SignInManager<AppUser> signIn,
        ITokenService tokens,
        IHostEnvironment env)
        : ControllerBase
    {
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Register(RegisterDto dto)
        {
            var user = new AppUser { 
            
                Email = dto.Email,
                UserName = dto.UserName,
                FirstName = dto.FirstName,
                LastName = dto.LastName
            };
            
            var normalized = dto.UserName.ToUpper();
            
            var existing = await users.Users
                .FirstOrDefaultAsync(u => u.NormalizedUserName == normalized);

            if (existing != null)
                return BadRequest("Username is already taken.");


            var result = await users.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors.Select(e => e.Description));

            // Optional: add default role
            // await _users.AddToRoleAsync(user, "User");

            return await tokens.CreateAuthResponseAsync(user);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Login(LoginDto dto)
        {
            var user = await users.FindByEmailAsync(dto.Email);
            if (user is null) return Unauthorized("Invalid credentials.");

            var result = await signIn.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);
            if (!result.Succeeded) return Unauthorized("Invalid credentials.");

            return await tokens.CreateAuthResponseAsync(user);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserDto>> Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Unauthorized();

            var user = await users.FindByIdAsync(userId);
            if (user is null) return Unauthorized();

            return new UserDto(user.Id, user.Email ?? "", user.UserName!, user.FirstName, user.LastName, user.ProfileImageUrl);
        }

        [HttpPost("password/forgot")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var user = await users.FindByEmailAsync(dto.Email);
            // Always 200 to avoid account enumeration
            if (user == null) return Ok(new { message = "If the account exists, a reset link has been sent." });

            var token = await users.GeneratePasswordResetTokenAsync(user);
            var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            // Build a link your mobile/web client can handle (adjust to your scheme/route)
            var link = $"{Request.Scheme}://{Request.Host}/reset-password?email={Uri.EscapeDataString(dto.Email)}&token={encoded}";

            // TODO: Send 'link' via your email service (IEmailSender, SendGrid, etc.)
            // For development: return token so you can test without email.
            if (env.IsDevelopment())
            {
                return Ok(new
                {
                    message = "Dev-only: use this token to call /auth/password/reset",
                    token = encoded,
                    link
                });
            }

            return Ok(new { message = "If the account exists, a reset link has been sent." });
        }

        // ===== 2) Complete reset with token =====
        [HttpPost("password/reset")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var user = await users.FindByEmailAsync(dto.Email);
            // Always 200 to avoid account enumeration
            if (user == null) return Ok(new { message = "Password has been reset if the account exists." });

            string decodedToken;
            try
            {
                decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(dto.Token));
            }
            catch
            {
                return BadRequest(new { error = "Invalid token format." });
            }

            var result = await users.ResetPasswordAsync(user, decodedToken, dto.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    errors = result.Errors.Select(e => new { e.Code, e.Description })
                });
            }

            return Ok(new { message = "Password reset successful." });
        }

        // ===== 3) Change password (authenticated) =====
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("password/change")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var user = await users.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var result = await users.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    errors = result.Errors.Select(e => new { e.Code, e.Description })
                });
            }

            return Ok(new { message = "Password changed." });
        }
    }
}
