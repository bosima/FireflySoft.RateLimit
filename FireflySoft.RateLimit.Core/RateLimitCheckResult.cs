using System;

namespace FireflySoft.RateLimit.Core
{
    public class RateLimitCheckResult<T>
    {
        public bool IsLimit { get; set; }

        public RateLimitRule<T> Rule { get; set; }
    }
}