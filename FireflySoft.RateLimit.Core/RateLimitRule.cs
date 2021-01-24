using System;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// The rule of rate limit
    /// </summary>
    public class RateLimitRule<TRequest>
    {
        /// <summary>
        /// identity
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The number of seconds locked after triggering rate limiting. 0 means not locked
        /// </summary>
        public int LockSeconds { get; set; }

        /// <summary>
        /// Extract the rate limit target from the instance of T, such as a value in HTTP Header. A fixed value can be returned to restrict the access of all users.
        /// Different target need to be used in different rules.
        /// </summary>
        public Func<TRequest, string> ExtractTarget { get; set; }

        /// <summary>
        /// Check whether the instance of T matches the rules. For example, you can check the path and HTTP Header in HttpContext.
        /// </summary>
        public Func<TRequest, bool> CheckRuleMatching { get; set; }
    }
}
