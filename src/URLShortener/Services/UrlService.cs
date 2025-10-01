using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Prometheus;
using StackExchange.Redis;
using System.Text.Json;
using URLShortener.Data;
using URLShortener.Models;
using URLShortener.Services;
using URLShortener.Utils;

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

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

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
            if (string.IsNullOrWhiteSpace(shortKey))
                return null;

            var mapping = await _cache.GetAsync<UrlMapping>(shortKey);
            if (mapping != null)
            {
                _logger.LogInformation("Cache hit for {ShortKey}", shortKey);
                return mapping;
            }

            mapping = await _db.UrlMappings.FirstOrDefaultAsync(u => u.ShortKey == shortKey, ct);
            if (mapping == null)
                return null;

            _logger.LogInformation("Cache miss for {ShortKey}, loaded from DB");

            await _cache.SetAsync(mapping.ShortKey, mapping, TimeSpan.FromMinutes(60));

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

            try
            {
                if (_cache != null)
                {
                    await _cache.RemoveAsync(shortKey);
                    await _cache.RemoveAsync($"hits:{shortKey}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove cache entries for {ShortKey}", shortKey);
            }

            return true;
        }

        public async Task<UrlMapping?> IncrementHitsAsync(string shortKey, CancellationToken ct = default)
        {
            var cachedHits = await _cache.GetStringAsync($"hits:{shortKey}");
            int hits;

            if (cachedHits != null && int.TryParse(cachedHits, out hits))
            {
                hits++;
                await _cache.SetStringAsync($"hits:{shortKey}", hits.ToString());
                UrlHits.Inc();

                var mDb = await _db.UrlMappings.FirstOrDefaultAsync(u => u.ShortKey == shortKey, ct);
                if (mDb != null)
                {
                    mDb.Hits = hits;
                    await _db.SaveChangesAsync(ct);

                    await _cache.SetStringAsync(shortKey, JsonSerializer.Serialize(mDb), TimeSpan.FromMinutes(10));

                    return mDb;
                }
            }
            else
            {
                var mDb = await _db.UrlMappings.FirstOrDefaultAsync(u => u.ShortKey == shortKey, ct);
                if (mDb == null)
                {
                    _logger.LogWarning("IncrementHitsAsync: short key not found: {ShortKey}", shortKey);
                    return null;
                }

                mDb.Hits++;
                hits = mDb.Hits;
                await _db.SaveChangesAsync(ct);

                await _cache.SetStringAsync($"hits:{shortKey}", hits.ToString());
                await _cache.SetStringAsync(shortKey, JsonSerializer.Serialize(mDb), TimeSpan.FromMinutes(10));
                UrlHits.Inc();

                return mDb;
            }

            var m = new UrlMapping
            {
                ShortKey = shortKey,
                Hits = hits
            };

            return m;
        }
    }

}
