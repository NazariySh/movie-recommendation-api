namespace MovieRecommendation.Application.Interfaces;

public interface ICacheService
{
    Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default);

    void Remove(string key);

    void RemoveByPrefix(string prefix);
}
