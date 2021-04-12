using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FireflySoft.RateLimit.Core.Rule;
using FireflySoft.RateLimit.Core.Time;

namespace FireflySoft.RateLimit.Core.InProcessAlgorithm
{
    /// <summary>
    /// Define an in-process leaky bucket algorithm
    /// </summary>
    public class InProcessLeakyBucketAlgorithm : BaseInProcessAlgorithm
    {
        /// <summary>
        /// create a new instance
        /// </summary>
        /// <param name="rules">The rate limit rules</param>
        /// <param name="timeProvider">The time provider</param>
        /// <param name="updatable">If rules can be updated</param>
        public InProcessLeakyBucketAlgorithm(IEnumerable<LeakyBucketRule> rules, ITimeProvider timeProvider = null, bool updatable = false)
        : base(rules, timeProvider, updatable)
        {
        }

        /// <summary>
        /// check single rule for target
        /// </summary>
        /// <param name="target"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        protected override RuleCheckResult CheckSingleRule(string target, RateLimitRule rule)
        {
            var currentRule = rule as LeakyBucketRule;
            var amount = 1;

            var result = InnerCheckSingleRule(target, amount, currentRule);
            return new RuleCheckResult()
            {
                IsLimit = result.Item1,
                Target = target,
                Count = result.Item2,
                Rule = rule,
                Wait = result.Item3
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        protected override async Task<RuleCheckResult> CheckSingleRuleAsync(string target, RateLimitRule rule)
        {
            return await Task.FromResult(CheckSingleRule(target, rule));
        }

        /// <summary>
        /// Increase the count value of the rate limit target for leaky bucket algorithm.
        /// </summary>
        /// <param name="target">The target</param>
        /// <param name="amount">amount of increase</param>
        /// <param name="currentRule">The current rule</param>
        /// <returns>Amount of request in the bucket</returns>
        public Tuple<bool, long, long> InnerCheckSingleRule(string target, long amount, LeakyBucketRule currentRule)
        {
            bool locked = CheckLocked(target);
            if (locked)
            {
                return Tuple.Create(true, -1L, -1L);
            }

            var outflowUnit = (int)currentRule.OutflowUnit.TotalMilliseconds;
            var currentTime = _timeProvider.GetCurrentLocalTime();

            lock (target)
            {
                var countData = _cache.GetCacheItem(target);
                if (countData == null)
                {
                    var startTime = AlgorithmStartTime.ToSpecifiedTypeTime(currentTime, TimeSpan.FromMilliseconds(outflowUnit), currentRule.StartTimeType);
                    _cache.Add(target, new CountValue(amount) { LastFlowTime = startTime }, DateTimeOffset.MaxValue);
                    return Tuple.Create(false, 0L, 0L);
                }

                var countValue = (CountValue)countData.Value;
                var lastTime = countValue.LastFlowTime;
                var pastTime = currentTime - lastTime;
                var lastTimeChanged = false;
                var pastTimeMilliseconds = pastTime.TotalMilliseconds;

                long newCount = 0;
                long wait = 0;
                if (pastTimeMilliseconds < outflowUnit)
                {
                    newCount = countValue.Value + amount;
                    if (newCount <= currentRule.Capacity + currentRule.OutflowQuantityPerUnit)
                    {
                        var currentUnitRestTime = outflowUnit - pastTimeMilliseconds;
                        wait = CalculateWaitTime(currentRule.OutflowQuantityPerUnit, outflowUnit, newCount, currentUnitRestTime);
                    }
                    else
                    {
                        if (currentRule.LockSeconds > 0)
                        {
                            TryLock(target, currentTime, TimeSpan.FromSeconds(currentRule.LockSeconds));
                        }
                        return Tuple.Create(true, currentRule.Capacity, -1L);
                    }
                }
                else
                {
                    var pastOutflowUnitQuantity = (int)(pastTimeMilliseconds / outflowUnit);
                    lastTime = lastTime.AddMilliseconds(pastOutflowUnitQuantity * outflowUnit);
                    lastTimeChanged = true;

                    if (countValue.Value < currentRule.OutflowQuantityPerUnit)
                    {
                        newCount = amount;
                        wait = 0;
                    }
                    else
                    {
                        var pastOutflowQuantity = currentRule.OutflowQuantityPerUnit * pastOutflowUnitQuantity;
                        newCount = countValue.Value - pastOutflowQuantity + amount;
                        newCount = newCount > 0 ? newCount : amount;
                        var currentUnitRestTime = outflowUnit - (currentTime - lastTime).TotalMilliseconds;
                        wait = CalculateWaitTime(currentRule.OutflowQuantityPerUnit, outflowUnit, newCount, currentUnitRestTime);
                    }
                }

                countValue.Value = newCount;
                if (lastTimeChanged)
                {
                    countValue.LastFlowTime = lastTime;
                }

                var viewCount = newCount - currentRule.OutflowQuantityPerUnit;
                viewCount = viewCount < 0 ? 0 : viewCount;
                return Tuple.Create(false, viewCount, wait);
            }
        }

        private static long CalculateWaitTime(long outflowQuantityPerUnit, long outflowUnit, long bucketCount, double currentUnitRestTime)
        {
            long wait = 0;
            if (bucketCount > outflowQuantityPerUnit)
            {
                var batchNumber = (int)Math.Ceiling(bucketCount / (double)outflowQuantityPerUnit) - 1;
                if (batchNumber == 1)
                {
                    wait = (long)currentUnitRestTime;
                }
                else
                {
                    wait = (long)(outflowUnit * (batchNumber - 1) + currentUnitRestTime);
                }
            }

            return wait;
        }
    }
}