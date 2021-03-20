using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// Leaky Bucket Algorithm
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    public class LeakyBucketAlgorithm<TRequest> : UpdatableRateLimitAlgorithm<TRequest>
    {
        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="rules"></param>
        /// <param name="updatable"></param>
        public LeakyBucketAlgorithm(IEnumerable<LeakyBucketRateLimitRule<TRequest>> rules, bool updatable = false)
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
            var currentRule = rule as LeakyBucketRateLimitRule<TRequest>;

            var result = storage.LeakyBucketIncrement(target, 1, currentRule.Capacity, (int)currentRule.OutflowUnit.TotalMilliseconds, currentRule.OutflowQuantityPerUnit, currentRule.StartTimeType, currentRule.LockSeconds);
            Debug.WriteLine("check result:" + result.Item1 + "," + result.Item2);
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
            var currentRule = rule as LeakyBucketRateLimitRule<TRequest>;

            var result = await storage.LeakyBucketIncrementAsync(target, 1, currentRule.Capacity, (int)currentRule.OutflowUnit.TotalMilliseconds, currentRule.OutflowQuantityPerUnit, currentRule.StartTimeType, currentRule.LockSeconds);
            Debug.WriteLine("check result:" + result.Item1 + "," + result.Item2);
            return result.Item1;
        }
    }
}
