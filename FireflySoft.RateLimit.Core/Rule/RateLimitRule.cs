using System;
using System.Threading.Tasks;

namespace FireflySoft.RateLimit.Core.Rule
{
    /// <summary>
    /// The rule of rate limit
    /// </summary>
    public abstract class RateLimitRule
    {
        /// <summary>
        /// The identity of the rule, required and cannot be duplicated within the storage currently in use.
        /// The default value is a Guid string.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The name of the rule.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The number of seconds locked after triggering rate limiting. 0 means not locked
        /// </summary>
        public int LockSeconds { get; set; }

        /// <summary>
        /// The type of statistics start time
        /// </summary>
        public StartTimeType StartTimeType { get; set; }

        /// <summary>
        /// Extract the rate limit target from the instance of T, such as a value in HTTP Header. A fixed value can be returned to restrict the access of all users.
        /// </summary>
        public Func<object, string> ExtractTarget { get; set; }

        /// <summary>
        /// Extract the rate limit target from the instance of T, such as a value in HTTP Header. A fixed value can be returned to restrict the access of all users.
        /// </summary>
        public Func<object, Task<string>> ExtractTargetAsync { get; set; }

        /// <summary>
        /// Check whether the instance of T matches the rules. For example, you can check the path and HTTP Header in HttpContext.
        /// </summary>
        public Func<object, bool> CheckRuleMatching { get; set; }

        /// <summary>
        /// Check whether the instance of T matches the rules. For example, you can check the path and HTTP Header in HttpContext.
        /// </summary>
        public Func<object, Task<bool>> CheckRuleMatchingAsync { get; set; }
    }
}
