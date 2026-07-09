using System.Text.Json;
using FinancialTransactions.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace FinancialTransactions.Infrastructure.Caching
{
    public sealed class RedisCacheService : ICacheService
    {
        private readonly string? _connectionString;
        private readonly ILogger<RedisCacheService> _logger;
        private ConnectionMultiplexer? _connection;

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public RedisCacheService(
            IConfiguration configuration,
            ILogger<RedisCacheService> logger)
        {
            _connectionString = configuration["Redis:ConnectionString"];
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
        {
            var database = await GetDatabaseAsync();

            if (database is null)
                return default;

            var value = await database.StringGetAsync(key);

            if (!value.HasValue)
                return default;

            return JsonSerializer.Deserialize<T>(value.ToString(), JsonOptions);
        }

        public async Task SetAsync<T>(
            string key,
            T value,
            TimeSpan expiration,
            CancellationToken cancellationToken)
        {
            var database = await GetDatabaseAsync();

            if (database is null)
                return;

            var serializedValue = JsonSerializer.Serialize(value, JsonOptions);

            await database.StringSetAsync(key, serializedValue, expiration);
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken)
        {
            var database = await GetDatabaseAsync();

            if (database is null)
                return;

            await database.KeyDeleteAsync(key);
        }

        private async Task<IDatabase?> GetDatabaseAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_connectionString))
                    return null;

                if (_connection is not null && _connection.IsConnected)
                    return _connection.GetDatabase();

                _connection = await ConnectionMultiplexer.ConnectAsync(_connectionString);

                return _connection.GetDatabase();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis is unavailable. Continuing without cache.");

                return null;
            }
        }
    }
}
