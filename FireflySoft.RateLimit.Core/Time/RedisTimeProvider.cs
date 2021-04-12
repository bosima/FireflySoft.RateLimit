using System;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace FireflySoft.RateLimit.Core.Time
{
    /// <summary>
    /// The time provider of redis
    /// </summary>
    public class RedisTimeProvider : ITimeProvider
    {
        private readonly ConnectionMultiplexer _redisClient;
        /// <summary>
        /// 
        /// </summary>
        public RedisTimeProvider(ConnectionMultiplexer redisClient)
        {
            _redisClient = redisClient;
        }

        /// <summary>
        /// Get current utc time
        /// </summary>
        /// <returns></returns>
        public DateTimeOffset GetCurrentUtcTime()
        {
            var server = GetRedisServer();
            return server.Time();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public long GetCurrentUtcMilliseconds()
        {
            DateTimeOffset utcTime = GetCurrentUtcTime();
            return utcTime.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// Get current local time
        /// </summary>
        /// <returns></returns>
        public DateTimeOffset GetCurrentLocalTime()
        {
            DateTimeOffset utcTime = GetCurrentUtcTime();
            return utcTime.ToLocalTime();
        }

        /// <summary>
        /// Get current local time
        /// </summary>
        /// <returns></returns>
        public async Task<DateTimeOffset> GetCurrentUtcTimeAsync()
        {
            var server = GetRedisServer();
            return await server.TimeAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Get the milliseconds of current unix time
        /// </summary>
        /// <returns></returns>
        public async Task<long> GetCurrentUtcMillisecondsAsync()
        {
            DateTimeOffset utcTime = await GetCurrentUtcTimeAsync().ConfigureAwait(false);
            return utcTime.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// Get current local time
        /// </summary>
        /// <returns></returns>
        public async Task<DateTimeOffset> GetCurrentLocalTimeAsync()
        {
            DateTimeOffset utcTime = await GetCurrentUtcTimeAsync().ConfigureAwait(false);
            return utcTime.ToLocalTime();
        }

        private IServer GetRedisServer()
        {
            var endPoints = _redisClient.GetEndPoints();
            foreach (var endPoint in endPoints)
            {
                var server = _redisClient.GetServer(endPoint);
                if (server.IsConnected)
                {
                    return server;
                }
            }

            throw new RedisException("could not found valid redis server.");
        }
    }
}