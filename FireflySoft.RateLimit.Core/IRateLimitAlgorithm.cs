using System;
namespace FireflySoft.RateLimit.Core
{
    public interface IRateLimitAlgorithm
    {
        bool Check(string target, IRateLimitStorage storage, RateLimitRuleBase rule);
    }
}
