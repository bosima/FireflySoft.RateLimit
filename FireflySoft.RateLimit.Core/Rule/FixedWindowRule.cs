using System;
using System.Collections.Generic;

namespace FireflySoft.RateLimit.Core.Rule
{
    /// <summary>
    /// Fixed Window Algorithm
    /// </summary>
    public class FixedWindowRule : RateLimitRule
    {
        /// <summary>
        /// The statistical time window, which counts the number of requests in this time.
        /// When using redis storage, it needs to be an integral multiple of one second.
        /// </summary>
        public TimeSpan StatWindow { get; set; }

        /// <summary>
        /// The threshold of triggering rate limiting in the statistical time window.
        /// If less than 0, it means no limit.
        /// </summary>
        public int LimitNumber { get; set; }

        /// <summary>
        /// Get the rate limit threshold.
        /// </summary>
        /// <returns></returns>
        public override long GetLimitThreshold()
        {
            return LimitNumber;
        }
    }
}