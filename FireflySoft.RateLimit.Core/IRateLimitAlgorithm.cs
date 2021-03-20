using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    ///  Defines a mechanism for using rate limit algorithm.
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    public interface IRateLimitAlgorithm<TRequest>
    {
        /// <summary>
        /// Check the request and return the rate limit result
        /// </summary>
        /// <param name="request"></param>
        /// <param name="storage"></param>
        /// <param name="timeProvider"></param>
        /// <returns></returns>
        List<RateLimitCheckResult<TRequest>> Check(TRequest request, IRateLimitStorage storage, ITimeProvider timeProvider);

        /// <summary>
        /// Check the request and return the rate limit result
        /// </summary>
        /// <param name="request"></param>
        /// <param name="storage"></param>
        /// <param name="timeProvider"></param>
        /// <returns></returns>
        Task<List<RateLimitCheckResult<TRequest>>> CheckAsync(TRequest request, IRateLimitStorage storage, ITimeProvider timeProvider);
    }
}
