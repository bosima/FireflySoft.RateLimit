using System;
using System.Collections.Generic;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// Fixed Window Algorithm
    /// </summary>
    public class FixedWindowRateLimitRule<T> : RateLimitRule<T>
    {
        /// <summary>
        /// Statistical time window, which counts the number of requests in this time.
        /// When using redis storage, it needs to be an integral multiple of one second.
        /// </summary>
        public TimeSpan StatWindow { get; set; }

        /// <summary>
        /// The threshold of triggering rate limit in the statistical time window。
        /// </summary>
        public int LimitNumber { get; set; }
    }
}