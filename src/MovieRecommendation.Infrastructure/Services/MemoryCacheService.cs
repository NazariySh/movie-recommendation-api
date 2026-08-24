using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using MovieRecommendation.Application.Interfaces;

namespace MovieRecommendation.Infrastructure.Services;

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, byte> _keys = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public MemoryCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out T? cached) && cached is not null)
        {
            return cached;
        }

        var gate = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (_cache.TryGetValue(key, out cached) && cached is not null)
            {
                return cached;
            }

            var value = await factory(cancellationToken);

            var options = new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl };
            options.RegisterPostEvictionCallback((evictedKey, _, _, _) => _keys.TryRemove(evictedKey.ToString()!, out _));

            _cache.Set(key, value, options);
            _keys[key] = 0;
            return value;
        }
        finally
        {
            gate.Release();
        }
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
        _keys.TryRemove(key, out _);
    }

    public void RemoveByPrefix(string prefix)
    {
        foreach (var key in _keys.Keys)
        {
            if (key.StartsWith(prefix, StringComparison.Ordinal))
            {
                _cache.Remove(key);
                _keys.TryRemove(key, out _);
            }
        }
    }
}
