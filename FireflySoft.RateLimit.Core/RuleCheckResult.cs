using System;
using FireflySoft.RateLimit.Core.Rule;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// Defines the result of single rule check
    /// </summary>
    public class RuleCheckResult
    {
        /// <summary>
        /// If true, it means that the current request should be limited
        /// </summary>
        /// <value></value>
        public bool IsLimit { get; set; }

        /// <summary>
        /// The time to open the next time window,
        /// or the time when the rate limiting lock ends.
        /// </summary>
        /// <value></value>
        public DateTimeOffset ResetTime { get; set; }

        /// <summary>
        /// The number of requests passed in the current time window.
        /// </summary>
        /// <value></value>
        public long Count { get; set; }

        /// <summary>
        /// The number of requests remaining in the current time window that will not be limited.
        /// 
        /// </summary>
        /// <value></value>
        public long Remaining { get; set; }

        /// <summary>
        /// The queue waiting time of the current request, which is only for the leaky bucket algorithm.
        /// With Task.Dealy, you can simulate queue processing requests.
        /// </summary>
        /// <value></value>
        public long Wait { get; set; } = -1;

        /// <summary>
        /// The current rate limit target
        /// </summary>
        /// <value></value>
        public string Target { get; set; }

        /// <summary>
        /// The current rule
        /// </summary>
        /// <value></value>
        public RateLimitRule Rule { get; set; }
    }
}