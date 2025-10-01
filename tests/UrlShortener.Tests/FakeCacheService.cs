using URLShortener.Services;

public class FakeCacheService : ICacheService
{
    private readonly Dictionary<string, string> _stringCache = new();
    private readonly Dictionary<string, object> _objectCache = new();

    public Task<T?> GetAsync<T>(string key)
    {
        if (_objectCache.TryGetValue(key, out var value) && value is T t)
            return Task.FromResult<T?>(t);
        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        _objectCache[key] = value!;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _objectCache.Remove(key);
        _stringCache.Remove(key);
        return Task.CompletedTask;
    }

    public Task<string?> GetStringAsync(string key)
    {
        _stringCache.TryGetValue(key, out var value);
        return Task.FromResult<string?>(value);
    }

    public Task SetStringAsync(string key, string value, TimeSpan? expiry = null)
    {
        _stringCache[key] = value;
        return Task.CompletedTask;
    }
}