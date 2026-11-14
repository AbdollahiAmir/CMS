using System.Text.Json;
using BackEnd.Data;
using BackEnd.IServices;
using BackEnd.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BackEnd.Services
{
    public class IntegrationSettingsService : IIntegrationSettingsService
    {
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        private readonly AppDbContext _context;
        private readonly IDataProtector _protector;
        private readonly IMemoryCache _cache;
        private readonly ILogger<IntegrationSettingsService> _logger;

        public IntegrationSettingsService(
            AppDbContext context,
            IDataProtectionProvider protectionProvider,
            IMemoryCache cache,
            ILogger<IntegrationSettingsService> logger)
        {
            _context = context;
            _protector = protectionProvider.CreateProtector("BackEnd.IntegrationSettings.v1");
            _cache = cache;
            _logger = logger;
        }

        private static string CacheKey(string key)
        {
            return $"integration:{key}";
        }

        public async Task<T> GetAsync<T>(string key) where T : class, new()
        {
            var cacheKey = CacheKey(key);

            if (_cache.TryGetValue(cacheKey, out string? cached) && cached is not null)
            {
                return JsonSerializer.Deserialize<T>(cached) ?? new T();
            }

            var row = await _context.IntegrationSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Key == key);

            var json = "{}";

            if (row is not null)
            {
                try
                {
                    json = _protector.Unprotect(row.ProtectedValue);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Integration setting {Key} could not be decrypted", key);
                }
            }

            _cache.Set(cacheKey, json, CacheDuration);

            return JsonSerializer.Deserialize<T>(json) ?? new T();
        }

        public async Task SaveAsync<T>(string key, T value, string? userId) where T : class
        {
            var json = JsonSerializer.Serialize(value);
            var protectedValue = _protector.Protect(json);

            var row = await _context.IntegrationSettings.FirstOrDefaultAsync(s => s.Key == key);

            if (row is null)
            {
                _context.IntegrationSettings.Add(new IntegrationSetting
                {
                    Key = key,
                    ProtectedValue = protectedValue,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedByUserId = userId
                });
            }
            else
            {
                row.ProtectedValue = protectedValue;
                row.UpdatedAt = DateTime.UtcNow;
                row.UpdatedByUserId = userId;
            }

            await _context.SaveChangesAsync();

            _cache.Remove(CacheKey(key));
        }
    }
}
