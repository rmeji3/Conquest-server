using Microsoft.EntityFrameworkCore;
using Conquest.Data.Auth;
using Conquest.Models.Users;
using Conquest.Services.Redis;

namespace Conquest.Services.Moderation
{
    public class BanningService(
        AuthDbContext context,
        IRedisService redis,
        ILogger<BanningService> logger) : IBanningService
    {
        private const string IpBanKeyPrefix = "banned:ip:";
        private const string UserBanKeyPrefix = "banned:user:";

        public async Task BanUserAsync(string userId, string reason, string? adminId = null)
        {
            var user = await context.Users.FindAsync(userId);
            if (user == null)
            {
                logger.LogWarning("Attempted to ban non-existent user {UserId}", userId);
                return;
            }

            if (user.IsBanned) return;

            user.IsBanned = true;
            user.BanReason = reason;
            user.BanCount++;
            
            // Log logic or AuditLog could go here
            logger.LogInformation("User {UserId} banned by {AdminId}. Reason: {Reason}. Ban Count: {Count}", 
                userId, adminId ?? "System", reason, user.BanCount);

            await context.SaveChangesAsync();

            // Cache in Redis
            // If we want indefinite keys or long expiry? Let's say 24 hours and refresh on access? 
            // Better: indefinite until unbanned.
            await redis.SetAsync($"{UserBanKeyPrefix}{userId}", reason);

            // Trigger IP ban if threshold met
            if (user.BanCount >= 3 && !string.IsNullOrEmpty(user.LastIpAddress))
            {
                logger.LogInformation("User {UserId} reached ban threshold. Banning IP {Ip}", userId, user.LastIpAddress);
                await BanIpAsync(user.LastIpAddress, $"Automatic ban due to user {userId} reaching ban threshold.");
            }
        }

        public async Task UnbanUserAsync(string userId, string? adminId = null)
        {
            var user = await context.Users.FindAsync(userId);
            if (user == null || !user.IsBanned) return;

            user.IsBanned = false;
            logger.LogInformation("User {UserId} unbanned by {AdminId}", userId, adminId ?? "System");
            
            await context.SaveChangesAsync();
            await redis.DeleteAsync($"{UserBanKeyPrefix}{userId}");
        }

        public async Task BanIpAsync(string ip, string reason, DateTime? expiresAt = null, string? adminId = null)
        {
            var existing = await context.IpBans.FindAsync(ip);
            if (existing != null) return; // Already banned

            var ban = new IpBan
            {
                IpAddress = ip,
                Reason = reason,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow
            };

            context.IpBans.Add(ban);
            await context.SaveChangesAsync();

            logger.LogInformation("IP {Ip} banned by {AdminId}. Reason: {Reason}", ip, adminId ?? "System", reason);

            if (expiresAt.HasValue)
            {
                var ttl = expiresAt.Value - DateTime.UtcNow;
                if (ttl > TimeSpan.Zero)
                {
                    await redis.SetAsync($"{IpBanKeyPrefix}{ip}", reason, ttl);
                }
            }
            else
            {
                await redis.SetAsync($"{IpBanKeyPrefix}{ip}", reason); // No expiry
            }
        }

        public async Task UnbanIpAsync(string ip, string? adminId = null)
        {
            var ban = await context.IpBans.FindAsync(ip);
            if (ban == null) return;

            context.IpBans.Remove(ban);
            await context.SaveChangesAsync();
            
            logger.LogInformation("IP {Ip} unbanned by {AdminId}", ip, adminId ?? "System");
            
            await redis.DeleteAsync($"{IpBanKeyPrefix}{ip}");
        }

        public async Task<(bool IsBanned, string? Reason)> CheckBanAsync(string ip, string? userId = null)
        {
            // Check IP First
            var ipBanReason = await redis.GetAsync<string>($"{IpBanKeyPrefix}{ip}");
            if (ipBanReason != null)
            {
                return (true, ipBanReason);
            }

            // Check User
            if (!string.IsNullOrEmpty(userId))
            {
                var userBanReason = await redis.GetAsync<string>($"{UserBanKeyPrefix}{userId}");
                if (userBanReason != null)
                {
                    return (true, userBanReason);
                }
            }
            
            return (false, null);
        }

        public async Task CheckReportThresholdAsync(string userId)
        {
            // This would likely query ReportService or Reports table
            // For now, we will leave this as a placeholder or simple implementation
            // passed on design.
            // "If user has > 10 reports, ban them"
            
            // Use AppDbContext? Wait, Reports are in AppDbContext, Users in AuthDbContext.
            // BanningService uses AuthDbContext.
            // We shouldn't mix contexts in one service ideally unless needed.
            // But we can just count?
            
            // For now, implementing as "placeholder" that logs
            logger.LogInformation("Checking report threshold for {UserId} (Not fully implemented yet)", userId);
            await Task.CompletedTask;
        }
        public async Task<Conquest.Dtos.Common.PagedResult<Conquest.Dtos.Moderation.BannedUserDto>> GetBannedUsersAsync(int page = 1, int limit = 20)
        {
             var query = context.Users.Where(u => u.IsBanned);
             var total = await query.CountAsync();
             
             var items = await query
                .OrderByDescending(u => u.BanCount) // Ordered by ban count or whatever makes sense
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(u => new Conquest.Dtos.Moderation.BannedUserDto(
                    u.Id,
                    u.UserName!,
                    u.Email!,
                    u.BanReason,
                    u.BanCount
                ))
                .ToListAsync();

             return new Conquest.Dtos.Common.PagedResult<Conquest.Dtos.Moderation.BannedUserDto>(items, total, page, limit);
        }

        public async Task<Conquest.Dtos.Moderation.BannedUserDto?> GetBannedUserAsync(string? userId = null, string? username = null, string? email = null)
        {
            var query = context.Users.AsQueryable();
            if (userId != null) query = query.Where(u => u.Id == userId);
            else if (username != null) query = query.Where(u => u.NormalizedUserName == username.ToUpper());
            else if (email != null) query = query.Where(u => u.NormalizedEmail == email.ToUpper());
            else return null;

            // Also check if they are actually banned? The requirement says "specific banned user".
            // If we find a user but they aren't banned, should we return null or the user?
            // Convention: GetBannedUser implies they must be banned.
            query = query.Where(u => u.IsBanned);

            var user = await query.FirstOrDefaultAsync();
            if (user == null) return null;

            return new Conquest.Dtos.Moderation.BannedUserDto(
                user.Id,
                user.UserName!,
                user.Email!,
                user.BanReason,
                user.BanCount
            );
        }
    }
}
