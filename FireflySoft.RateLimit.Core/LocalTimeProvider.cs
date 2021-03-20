using System;
using System.Threading.Tasks;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// Local time provider
    /// </summary>
    public class LocalTimeProvider : ITimeProvider
    {
        /// <summary>
        /// Get current utc time
        /// </summary>
        /// <returns></returns>
        public DateTimeOffset GetCurrentUtcTime()
        {
            return DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Get the milliseconds of current unix time
        /// </summary>
        /// <returns></returns>
        public long GetCurrentUnixTimeMilliseconds()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// Get current local time
        /// </summary>
        /// <returns></returns>
        public DateTimeOffset GetCurrentLocalTime()
        {
            return DateTimeOffset.Now;
        }

        /// <summary>
        /// Get current utc time
        /// </summary>
        /// <returns></returns>
        public async Task<DateTimeOffset> GetCurrentUtcTimeAsync()
        {
            return await Task.FromResult(GetCurrentUtcTime());
        }

        /// <summary>
        /// Get the milliseconds of current unix time
        /// </summary>
        /// <returns></returns>
        public async Task<long> GetCurrentUnixTimeMillisecondsAsync()
        {
            return await Task.FromResult(GetCurrentUnixTimeMilliseconds());
        }

        /// <summary>
        /// Get current local time
        /// </summary>
        /// <returns></returns>
        public async Task<DateTimeOffset> GetCurrentLocalTimeAsync()
        {
            return await Task.FromResult(GetCurrentLocalTime());
        }
    }
}