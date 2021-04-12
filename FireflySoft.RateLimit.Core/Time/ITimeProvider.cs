using System;
using System.Threading.Tasks;

namespace FireflySoft.RateLimit.Core.Time
{
    /// <summary>
    /// The interface for time provider
    /// </summary>
    public interface ITimeProvider
    {
        /// <summary>
        /// Get the milliseconds of current unix time
        /// </summary>
        /// <returns></returns>
        long GetCurrentUtcMilliseconds();

        /// <summary>
        /// Get current utc time
        /// </summary>
        /// <returns></returns>
        DateTimeOffset GetCurrentUtcTime();

        /// <summary>
        /// Get current local time
        /// </summary>
        /// <returns></returns>
        DateTimeOffset GetCurrentLocalTime();

        /// <summary>
        /// Get the milliseconds of current unix time
        /// </summary>
        /// <returns></returns>
        Task<long> GetCurrentUtcMillisecondsAsync();

        /// <summary>
        /// Get current local time
        /// </summary>
        /// <returns></returns>
        Task<DateTimeOffset> GetCurrentLocalTimeAsync();

        /// <summary>
        /// Get current utc time
        /// </summary>
        /// <returns></returns>
        Task<DateTimeOffset> GetCurrentUtcTimeAsync();
    }
}