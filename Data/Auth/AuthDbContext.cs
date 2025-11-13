namespace Conquest.Data.Auth
{
    using Conquest.Models.AppUsers;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;

    public class AuthDbContext : IdentityDbContext<AppUser>
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }
    }
}
