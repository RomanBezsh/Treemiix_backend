using CloneAmazonBack.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace CloneAmazonBack.Services;

public class NullCacheService : ICacheService
{
    private readonly ILogger<NullCacheService> _logger;

    public NullCacheService(ILogger<NullCacheService> logger)
    {
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        _logger.LogTrace("Cache disabled - GET miss for key: {Key}", key);
        return Task.FromResult<T?>(null);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
    {
        _logger.LogTrace("Cache disabled - SET ignored for key: {Key}", key);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Cache disabled - REMOVE ignored for key: {Key}", key);
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Cache disabled - REMOVE BY PREFIX ignored for prefix: {Prefix}", prefix);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }
}