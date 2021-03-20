using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// Sliding Window Algorithm
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    public class SlidingWindowAlgorithm<TRequest> : UpdatableRateLimitAlgorithm<TRequest>
    {
        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="rules"></param>
        /// <param name="updatable"></param>
        public SlidingWindowAlgorithm(IEnumerable<SlidingWindowRateLimitRule<TRequest>> rules, bool updatable = false)
        : base(rules, updatable)
        {
        }

        /// <summary>
        /// check single rule for target
        /// </summary>
        /// <param name="target"></param>
        /// <param name="storage"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        protected override bool CheckSingleRule(string target, IRateLimitStorage storage, RateLimitRule<TRequest> rule)
        {
            var currentRule = rule as SlidingWindowRateLimitRule<TRequest>;
            var result = storage.SlidingWindowIncrement(target, 1, currentRule.StatWindow, currentRule.StatPeriod, rule.StartTimeType, currentRule.PeriodNumber, currentRule.LimitNumber, currentRule.LockSeconds);
            Debug.WriteLine($"check result:{result.Item1},{result.Item2},{DateTimeOffset.Now.ToUnixTimeMilliseconds()}");
            return result.Item1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="storage"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        protected override async Task<bool> CheckSingleRuleAsync(string target, IRateLimitStorage storage, RateLimitRule<TRequest> rule)
        {
            var currentRule = rule as SlidingWindowRateLimitRule<TRequest>;
            var result = await storage.SlidingWindowIncrementAsync(target, 1, currentRule.StatWindow, currentRule.StatPeriod, rule.StartTimeType, currentRule.PeriodNumber, currentRule.LimitNumber, currentRule.LockSeconds);
            Debug.WriteLine($"check result:{result.Item1},{result.Item2},{DateTimeOffset.Now.ToUnixTimeMilliseconds()}");
            return result.Item1;
        }
    }
}
