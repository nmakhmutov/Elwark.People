using System;
using System.Threading.Tasks;
using Elwark.People.Shared;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Elwark.People.Infrastructure.Cache
{
    public class CacheStorage : ICacheStorage
    {
        private readonly IDatabaseAsync _database;
        private readonly ILogger<CacheStorage> _logger;

        public CacheStorage(IConnectionMultiplexer multiplexer, ILogger<CacheStorage> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _database = multiplexer.GetDatabase() ?? throw new ArgumentNullException(nameof(multiplexer));
        }

        public Task<bool> CreateAsync<T>(string key, T data, TimeSpan expiry)
        {
            _logger.LogDebug("Creating {key} with expiration {expiry} for data {@data}", key, expiry, data);
            return _database.StringSetAsync(key, JsonConvert.SerializeObject(data, ElwarkJsonSettings.Value), expiry);
        }

        public async Task<T?> ReadAsync<T>(string key) where T : class
        {
            var result = await _database.StringGetAsync(key);
            _logger.LogDebug("For {key} data is {@data}", key, result.ToString());

            return result.HasValue
                ? JsonConvert.DeserializeObject<T>(result, ElwarkJsonSettings.Value)
                : null;
        }

        public Task<bool> DeleteAsync(string key)
        {
            _logger.LogDebug("Removing key {key}", key);
            return _database.KeyDeleteAsync(key);
        }
    }
}