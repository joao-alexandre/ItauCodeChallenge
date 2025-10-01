using URLShortener.Models;

namespace URLShortener.Services;

public interface IUrlService
{
    Task<UrlMapping> CreateAsync(string originalUrl, DateTimeOffset? expiresAt = null, CancellationToken ct = default);
    Task<UrlMapping?> GetByShortKeyAsync(string shortKey, CancellationToken ct = default);
    Task<UrlMapping?> IncrementHitsAsync(string shortKey, CancellationToken ct = default);
    Task<bool> DeleteByShortKeyAsync(string shortKey, CancellationToken ct = default);
}