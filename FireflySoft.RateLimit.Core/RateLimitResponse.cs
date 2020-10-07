using System;
namespace FireflySoft.RateLimit.Core
{
    public class RateLimitResponse
    {
        public bool IsLimit { get; set; }
        public RateLimitError Error { get; set; }
        public RateLimitRuleBase Rule { get; set; }
    }
}
