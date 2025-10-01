using StackExchange.Redis;
using System.Text.Json;
using URLShortener.Services;

namespace URLShortener.Services
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDatabase _db;
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        public RedisCacheService(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var value = await _db.StringGetAsync(key).ConfigureAwait(false);
            if (value.IsNullOrEmpty) return default;
            return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            var json = JsonSerializer.Serialize(value, _jsonOptions);
            await _db.StringSetAsync(key, json, expiry).ConfigureAwait(false);
        }

        public async Task RemoveAsync(string key)
        {
            await _db.KeyDeleteAsync(key).ConfigureAwait(false);
        }

        public async Task SetStringAsync(string key, string value, TimeSpan? expiry = null)
        {
            await _db.StringSetAsync(key, value, expiry).ConfigureAwait(false);
        }

        public async Task<string?> GetStringAsync(string key)
        {
            var value = await _db.StringGetAsync(key).ConfigureAwait(false);
            if (!value.HasValue) return null;
            return value;
        }
    }
}
