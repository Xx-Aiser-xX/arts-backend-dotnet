namespace Arts.Application.Interfaces;

public interface ICacheService
{
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken ct = default);
    Task<T> GetOrCreateAsync<T>(string key, string policyName, Func<Task<T>> factory, CancellationToken ct = default);

    Task RemoveAsync(string key, CancellationToken ct = default);

    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);
}