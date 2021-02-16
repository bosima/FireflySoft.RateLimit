using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// Fixed Window Algorithm
    /// </summary>
    public class FixedWindowAlgorithm<TRequest> : UpdatableRateLimitAlgorithm<TRequest>
    {
        /// <summary>
        /// create a new instance
        /// </summary>
        /// <param name="rules">rate limit rules</param>
        /// <param name="updatable">if rules can be updated</param>
        public FixedWindowAlgorithm(IEnumerable<FixedWindowRateLimitRule<TRequest>> rules, bool updatable = false)
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
            var currentRule = rule as FixedWindowRateLimitRule<TRequest>;
            var countAmount = 1;
            var expireTimeSpan = currentRule.StatWindow;
            var result = storage.FixedWindowIncrement(target, countAmount, expireTimeSpan, currentRule.LimitNumber, rule.LockSeconds);
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
            var currentRule = rule as FixedWindowRateLimitRule<TRequest>;
            var countAmount = 1;
            var expireTimeSpan = currentRule.StatWindow;
            var result = await storage.FixedWindowIncrementAsync(target, countAmount, expireTimeSpan, currentRule.LimitNumber, rule.LockSeconds);
            return result.Item1;
        }
    }
}

