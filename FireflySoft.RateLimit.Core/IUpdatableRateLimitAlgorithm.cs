using System;
using System.Collections.Generic;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    ///  Defines a mechanism for using rate limit algorithm, and the rules of the algorithm can be updated.
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    public interface IUpdatableRateLimitAlgorithm<TRequest> : IRateLimitAlgorithm<TRequest>
    {
        /// <summary>
        /// update the rate limit rules
        /// </summary>
        /// <param name="rules"></param>
        void UpdateRules(IEnumerable<RateLimitRule<TRequest>> rules);
    }
}
