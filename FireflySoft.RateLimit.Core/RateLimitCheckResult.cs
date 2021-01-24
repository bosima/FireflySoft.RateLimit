using System;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// Defines the result of rate limit check
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RateLimitCheckResult<T>
    {
        /// <summary>
        /// If true, it means that the current request should be limited
        /// </summary>
        /// <value></value>
        public bool IsLimit { get; set; }

        /// <summary>
        /// the rate limit target from the instance of T
        /// </summary>
        /// <value></value>
        public string Target{get;set;}

        /// <summary>
        /// The rule that trigger current limiting
        /// </summary>
        /// <value></value>
        public RateLimitRule<T> Rule { get; set; }
    }
}