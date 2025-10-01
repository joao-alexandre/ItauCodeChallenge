using Microsoft.EntityFrameworkCore;
using URLShortener.Services;
using URLShortener.Models;
using URLShortener.Utils;
using URLShortener.Data;
using Prometheus;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace URLShortener.Services
{
    public class UrlService : IUrlService
    {
        private static readonly Counter UrlCreated = Metrics.CreateCounter(
        "urlshortener_urls_created_total", "Total URLs created");

        private static readonly Counter UrlDeleted = Metrics.CreateCounter(
        "urlshortener_urls_deleted_total", "Total URLs deleted");

        private static readonly Counter UrlHits = Metrics.CreateCounter(
            "urlshortener_urls_hits_total", "Total URL hits");

        private readonly AppDbContext _db;
        private readonly ILogger<UrlService> _logger;
        public UrlService(AppDbContext db, ILogger<UrlService> logger)
        {
            _db = db;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private readonly ICacheService _cache;

        public UrlService(AppDbContext db, ILogger<UrlService> logger, ICacheService cache)
        {
            _db = db;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public async Task<UrlMapping> CreateAsync(string originalUrl, DateTimeOffset? expiresAt = null, CancellationToken ct = default)
        {
            _logger.LogInformation("Creating short URL for {Url}", originalUrl);

            var existing = await _db.UrlMappings.FirstOrDefaultAsync(u => u.OriginalUrl == originalUrl, ct);
            if (existing != null) return existing;

            const int maxAttempts = 5;

            for (int i = 0; i < maxAttempts; i++)
            {
                var key = ShortKeyGenerator.Generate();
                _logger.LogDebug("Attempt {Attempt}: generated key {Key}", i + 1, key);

                if (!await _db.UrlMappings.AnyAsync(u => u.ShortKey == key, ct))
                {
                    UrlCreated.Inc();
                    var m = new UrlMapping { ShortKey = key, OriginalUrl = originalUrl, ExpiresAt = expiresAt };
                    _db.UrlMappings.Add(m);
                    await _db.SaveChangesAsync(ct);
                    _logger.LogInformation("UrlMapping saved with ShortKey {ShortKey}", m.ShortKey);
                    await _cache.SetAsync(m.ShortKey, m, TimeSpan.FromMinutes(60));
                    return m;
                }
            }
            _logger.LogError("Failed to generate unique short key after {Attempts} attempts", maxAttempts);
            throw new InvalidOperationException("Could not generate unique short key");
        }

        public async Task<UrlMapping?> GetByShortKeyAsync(string shortKey, CancellationToken ct = default)
        {
            var cached = await _cache.GetStringAsync(shortKey);

            if (cached != null)
            {
                _logger.LogInformation("Cache hit for {ShortKey}", shortKey);
                return JsonSerializer.Deserialize<UrlMapping>(cached!);
            }

            var mapping = await _db.UrlMappings.FirstOrDefaultAsync(u => u.ShortKey == shortKey, ct);

            if (mapping != null)
            {
                await _cache.SetStringAsync(shortKey, JsonSerializer.Serialize(mapping), TimeSpan.FromMinutes(10));
                _logger.LogInformation("Cache miss for {ShortKey}, loaded from DB", shortKey);
            }

            return mapping;
        }

        public async Task<bool> DeleteByShortKeyAsync(string shortKey, CancellationToken ct = default)
        {
            _logger.LogInformation("Deleting short URL with key {ShortKey}", shortKey);
            var m = await _db.UrlMappings.FirstOrDefaultAsync(u => u.ShortKey == shortKey, ct);
            if (m == null) return false;
            _db.UrlMappings.Remove(m);
            await _db.SaveChangesAsync(ct);
            UrlDeleted.Inc();
            return true;
        }

        public async Task<UrlMapping?> IncrementHitsAsync(string shortKey, CancellationToken ct = default)
        {
            var m = await _db.UrlMappings.FirstOrDefaultAsync(u => u.ShortKey == shortKey, ct);

            if (m == null)
            {
                _logger.LogWarning("IncrementHitsAsync: short key not found: {ShortKey}", shortKey);
                return null;
            }

            m.Hits++;
            UrlHits.Inc();
            await _db.SaveChangesAsync(ct);
            return m;
        }
    }

}
