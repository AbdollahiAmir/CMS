using BackEnd.Data;
using BackEnd.DTOs.Auth;
using BackEnd.Helpers;
using BackEnd.IServices;
using BackEnd.Models;
using Microsoft.EntityFrameworkCore;
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace BackEnd.Services
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private static readonly TimeSpan ReuseGracePeriod = TimeSpan.FromSeconds(15);

        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly TimeProvider _time;

        public RefreshTokenService(AppDbContext context, IConfiguration config, TimeProvider time)
        {
            _context = context;
            _config = config;
            _time = time;
        }

        private TimeSpan Lifetime => TimeSpan.FromDays(_config.GetValue("Jwt:RefreshTokenDays", 14));

        private TimeSpan MaxSessionAge => TimeSpan.FromDays(_config.GetValue("Jwt:RefreshTokenMaxDays", 60));

        public static string Hash(string token)
        {
            return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        }

        private static string NewToken()
        {
            return Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(64));
        }

        private DateTime Now()
        {
            return _time.GetUtcNow().UtcDateTime;
        }

        public async Task<IssuedRefreshToken> IssueAsync(string userId)
        {
            var now = Now();
            var token = NewToken();
            var expiresAt = now + Lifetime;

            _context.RefreshTokens.Add(new RefreshToken
            {
                UserId = userId,
                TokenHash = Hash(token),
                FamilyId = Guid.NewGuid(),
                FamilyCreatedAt = now,
                CreatedAt = now,
                ExpiresAt = expiresAt
            });

            await _context.SaveChangesAsync();

            return new IssuedRefreshToken(token, expiresAt);
        }

        public async Task<RefreshRotationResult> RotateAsync(string? rawToken)
        {
            if (string.IsNullOrWhiteSpace(rawToken) || rawToken.Length > 200)
            {
                return RefreshRotationResult.Failed();
            }

            var now = Now();
            var hash = Hash(rawToken);

            var current = await _context.RefreshTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TokenHash == hash);

            if (current is null)
            {
                return RefreshRotationResult.Failed();
            }

            if (current.RevokedAt is not null)
            {
                var isBenignRace = current.ReplacedByTokenHash is not null
                    && now - current.RevokedAt.Value <= ReuseGracePeriod;

                if (!isBenignRace)
                {
                    await RevokeFamilyAsync(current.FamilyId, now);
                }

                return RefreshRotationResult.Failed();
            }

            var sessionEnd = current.FamilyCreatedAt + MaxSessionAge;

            if (current.ExpiresAt <= now || sessionEnd <= now)
            {
                return RefreshRotationResult.Failed();
            }

            var newToken = NewToken();
            var newHash = Hash(newToken);
            var newExpiresAt = now + Lifetime < sessionEnd ? now + Lifetime : sessionEnd;
            var currentId = current.Id;
            var rotated = false;

            await _context.ExecuteInTransactionAsync(async () =>
            {
                rotated = false;
                _context.ChangeTracker.Clear();

                var updated = await _context.RefreshTokens
                    .Where(t => t.Id == currentId && t.RevokedAt == null)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(t => t.RevokedAt, now)
                        .SetProperty(t => t.ReplacedByTokenHash, newHash));

                if (updated == 0)
                {
                    return;
                }

                _context.RefreshTokens.Add(new RefreshToken
                {
                    UserId = current.UserId,
                    TokenHash = newHash,
                    FamilyId = current.FamilyId,
                    FamilyCreatedAt = current.FamilyCreatedAt,
                    CreatedAt = now,
                    ExpiresAt = newExpiresAt
                });

                await _context.SaveChangesAsync();

                rotated = true;
            });

            if (!rotated)
            {
                return RefreshRotationResult.Failed();
            }

            return new RefreshRotationResult
            {
                Succeeded = true,
                UserId = current.UserId,
                Token = new IssuedRefreshToken(newToken, newExpiresAt)
            };
        }

        public async Task RevokeSessionAsync(string? rawToken)
        {
            if (string.IsNullOrWhiteSpace(rawToken) || rawToken.Length > 200)
            {
                return;
            }

            var hash = Hash(rawToken);

            var familyId = await _context.RefreshTokens
                .AsNoTracking()
                .Where(t => t.TokenHash == hash)
                .Select(t => (Guid?)t.FamilyId)
                .FirstOrDefaultAsync();

            if (familyId.HasValue)
            {
                await RevokeFamilyAsync(familyId.Value, Now());
            }
        }

        public async Task RevokeAllAsync(string userId)
        {
            var now = Now();

            await _context.RefreshTokens
                .Where(t => t.UserId == userId && t.RevokedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now));
        }

        public async Task<int> CleanupAsync()
        {
            var now = Now();
            var expiredBefore = now.AddDays(-1);
            var revokedBefore = now.AddDays(-7);

            return await _context.RefreshTokens
                .Where(t => t.ExpiresAt < expiredBefore || (t.RevokedAt != null && t.RevokedAt < revokedBefore))
                .ExecuteDeleteAsync();
        }

        private async Task RevokeFamilyAsync(Guid familyId, DateTime now)
        {
            await _context.RefreshTokens
                .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now));
        }
    }
}