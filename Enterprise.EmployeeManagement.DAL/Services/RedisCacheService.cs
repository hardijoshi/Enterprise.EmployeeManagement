using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Enterprise.EmployeeManagement.DAL.Services
{
    public class RedisCacheService : IRedisCacheService
    {
        private readonly IConnectionMultiplexer _redisConnection;
        private readonly ILogger<RedisCacheService> _logger;

        public RedisCacheService(IConnectionMultiplexer redisConnection, ILogger<RedisCacheService> logger)
        {
            _redisConnection = redisConnection;
            _logger = logger;
        }

        public async Task<T> GetAsync<T>(string key) where T : class
        {
            try
            {
                var db = _redisConnection.GetDatabase();
                var hashEntries = await db.HashGetAllAsync(key);

                if (hashEntries.Length == 0)
                    return null!;

                return ConvertToObject<T>(hashEntries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving data from Redis for key: {Key}", key);
                return null!;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
        {
            try
            {
                var db = _redisConnection.GetDatabase();
                var hashEntries = ConvertToHashEntries(value);

                await db.HashSetAsync(key, hashEntries);
                if (expiry.HasValue)
                {
                    await db.KeyExpireAsync(key, expiry.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting data in Redis for key: {Key}", key);
                throw;
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                var db = _redisConnection.GetDatabase();
                await db.KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing data from Redis for key: {Key}", key);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            var db = _redisConnection.GetDatabase();
            return await db.KeyExistsAsync(key);
        }

        private static HashEntry[] ConvertToHashEntries<T>(T value) where T : class
        {
            var properties = typeof(T).GetProperties();
            var entries = new List<HashEntry>();

            foreach (var prop in properties)
            {
                var propValue = prop.GetValue(value)?.ToString() ?? string.Empty;
                entries.Add(new HashEntry(prop.Name, propValue));
            }

            return entries.ToArray();
        }

        private static T ConvertToObject<T>(HashEntry[] hashEntries) where T : class
        {
            var obj = Activator.CreateInstance<T>();
            var properties = typeof(T).GetProperties();

            foreach (var prop in properties)
            {
                var entry = hashEntries.FirstOrDefault(x => x.Name == prop.Name);
                if (entry.Equals(new HashEntry())) continue;

                var value = entry.Value.ToString();
                if (string.IsNullOrEmpty(value)) continue;

                if (prop.PropertyType.IsEnum)
                {
                    prop.SetValue(obj, Enum.Parse(prop.PropertyType, value));
                }
                else if (prop.PropertyType == typeof(int))
                {
                    prop.SetValue(obj, int.Parse(value));
                }
                else
                {
                    prop.SetValue(obj, value);
                }
            }

            return obj;
        }
    }
}
