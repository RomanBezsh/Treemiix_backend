using CloneAmazonBack.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;

namespace CloneAmazonBack.Services;

public class CacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly RedisCacheOptions _options;
    private readonly ILogger<CacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public CacheService(
        IConnectionMultiplexer redis,
        IOptions<RedisCacheOptions> options,
        ILogger<CacheService> logger)
    {
        _redis = redis;
        _database = redis.GetDatabase();
        _options = options.Value;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (!_options.Enabled)
            return null;

        try
        {
            var fullKey = $"{_options.InstanceName}{key}";
            var value = await _database.StringGetAsync(fullKey);
            
            if (!value.HasValue)
                return null;

            _logger.LogDebug("Cache HIT for key: {Key}", fullKey);
            return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache GET failed for key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
    {
        if (!_options.Enabled)
            return;

        try
        {
            var fullKey = $"{_options.InstanceName}{key}";
            var json = JsonSerializer.Serialize(value, _jsonOptions);
            var ttl = expiry ?? TimeSpan.FromMinutes(_options.DefaultExpiryMinutes);
            
            await _database.StringSetAsync(fullKey, json, ttl);
            _logger.LogDebug("Cache SET for key: {Key}, TTL: {TTL}", fullKey, ttl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache SET failed for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return;

        try
        {
            var fullKey = $"{_options.InstanceName}{key}";
            await _database.KeyDeleteAsync(fullKey);
            _logger.LogDebug("Cache REMOVE for key: {Key}", fullKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache REMOVE failed for key: {Key}", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return;

        try
        {
            var fullPrefix = $"{_options.InstanceName}{prefix}";
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            
            await foreach (var key in server.KeysAsync(pattern: $"{fullPrefix}*"))
            {
                await _database.KeyDeleteAsync(key);
            }
            
            _logger.LogDebug("Cache REMOVE BY PREFIX: {Prefix}", fullPrefix);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache REMOVE BY PREFIX failed for prefix: {Prefix}", prefix);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return false;

        try
        {
            var fullKey = $"{_options.InstanceName}{key}";
            return await _database.KeyExistsAsync(fullKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache EXISTS failed for key: {Key}", key);
            return false;
        }
    }
}