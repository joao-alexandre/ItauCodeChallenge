using Microsoft.EntityFrameworkCore;
using URLShortener.Services;
using URLShortener.Models;
using URLShortener.Utils;
using URLShortener.Data;

namespace URLShortener.Services
{
    public class UrlService : IUrlService
    {
        private readonly AppDbContext _db;
        public UrlService(AppDbContext db) { _db = db; }

        public async Task<UrlMapping> CreateAsync(string originalUrl, DateTimeOffset? expiresAt = null, CancellationToken ct = default)
        {
            var existing = await _db.UrlMappings.FirstOrDefaultAsync(u => u.OriginalUrl == originalUrl, ct);
            if (existing != null) return existing;

            const int maxAttempts = 5;
            for (int i = 0; i < maxAttempts; i++)
            {
                var key = ShortKeyGenerator.Generate();
                if (!await _db.UrlMappings.AnyAsync(u => u.ShortKey == key, ct))
                {
                    var m = new UrlMapping { ShortKey = key, OriginalUrl = originalUrl, ExpiresAt = expiresAt };
                    _db.UrlMappings.Add(m);
                    await _db.SaveChangesAsync(ct);
                    return m;
                }
            }
            throw new InvalidOperationException("Could not generate unique short key");
        }

        public Task<UrlMapping?> GetByShortKeyAsync(string shortKey, CancellationToken ct = default)
            => _db.UrlMappings.FirstOrDefaultAsync(u => u.ShortKey == shortKey, ct);

        public async Task<bool> DeleteByShortKeyAsync(string shortKey, CancellationToken ct = default)
        {
            var m = await _db.UrlMappings.FirstOrDefaultAsync(u => u.ShortKey == shortKey, ct);
            if (m == null) return false;
            _db.UrlMappings.Remove(m);
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<UrlMapping?> IncrementHitsAsync(string shortKey, CancellationToken ct = default)
        {
            var m = await _db.UrlMappings.FirstOrDefaultAsync(u => u.ShortKey == shortKey, ct);
            if (m == null) return null;
            m.Hits++;
            await _db.SaveChangesAsync(ct);
            return m;
        }
    }

}
