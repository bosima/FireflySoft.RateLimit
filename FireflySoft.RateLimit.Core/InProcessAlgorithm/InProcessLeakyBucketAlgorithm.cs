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
        readonly CounterDictionary<LeakyBucketCounter> _leakyBuckets;

        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="rules">The rate limit rules</param>
        /// <param name="timeProvider">The time provider</param>
        /// <param name="updatable">If rules can be updated</param>
        public InProcessLeakyBucketAlgorithm(IEnumerable<LeakyBucketRule> rules, ITimeProvider timeProvider = null, bool updatable = false)
        : base(rules, timeProvider, updatable)
        {
            _leakyBuckets = new CounterDictionary<LeakyBucketCounter>(_timeProvider);
        }

        /// <summary>
        /// Check single rule for target
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
                IsLimit = result.IsLimit,
                Target = target,
                Count = result.Count,
                Rule = rule,
                Wait = result.Wait,
                ResetTime = result.ResetTime,
            };
        }

        /// <summary>
        /// Check single rule for target
        /// </summary>
        /// <param name="target"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        protected override async Task<RuleCheckResult> CheckSingleRuleAsync(string target, RateLimitRule rule)
        {
            return await Task.FromResult(CheckSingleRule(target, rule)).ConfigureAwait(false);
        }

        /// <summary>
        /// Increase the count value of the rate limit target for leaky bucket algorithm.
        /// </summary>
        /// <param name="target">The target</param>
        /// <param name="amount">amount of increase</param>
        /// <param name="currentRule">The current rule</param>
        /// <returns>Amount of request in the bucket</returns>
        public (bool IsLimit, long Count, long Wait, DateTimeOffset ResetTime) InnerCheckSingleRule(string target, long amount, LeakyBucketRule currentRule)
        {
            bool locked = CheckLocked(target, out DateTimeOffset? expireTime);
            if (locked)
            {
                return (true, -1L, -1L, expireTime.Value);
            }

            var currentTime = _timeProvider.GetCurrentLocalTime();
            (bool IsLimit, long Count, long Wait, DateTimeOffset ResetTime) countResult;

            lock (target)
            {
                countResult = Count(target, amount, currentRule, currentTime);
            }

            // do free lock
            var checkResult = countResult.IsLimit;
            if (checkResult)
            {
                if (currentRule.LockSeconds > 0)
                {
                    expireTime = currentTime.AddSeconds(currentRule.LockSeconds);
                    TryLock(target, expireTime.Value);
                    return (true, countResult.Count, -1L, expireTime.Value);
                }
            }

            return countResult;
        }

        private (bool IsLimit, long Count, long Wait, DateTimeOffset ResetTime) Count(string target, long amount, LeakyBucketRule currentRule, DateTimeOffset currentTime)
        {
            if (!_leakyBuckets.TryGet(target, out var cacheItem))
            {
                cacheItem = AddNewBucket(target, amount, currentRule, currentTime);
                return (false, amount, 0L, cacheItem.Counter.LastFlowOutTime.Add(currentRule.OutflowUnit));
            }

            var counter = (LeakyBucketCounter)cacheItem.Counter;
            var countValue = counter.Value;
            var lastFlowOutTime = counter.LastFlowOutTime;
            var pastMilliseconds = (currentTime - lastFlowOutTime).TotalMilliseconds;
            var outflowUnitMilliseconds = (int)currentRule.OutflowUnit.TotalMilliseconds;

            // After several time windows, some requests flow out,
            // and the number of requests in the leaky bucket needs to be recalculated
            if (pastMilliseconds >= outflowUnitMilliseconds)
            {
                var pastOutflowUnitQuantity = (int)(pastMilliseconds / outflowUnitMilliseconds);
                if (countValue < currentRule.OutflowQuantityPerUnit)
                {
                    countValue = 0;
                }
                else
                {
                    var pastOutflowQuantity = currentRule.OutflowQuantityPerUnit * pastOutflowUnitQuantity;
                    countValue = countValue - pastOutflowQuantity;
                    countValue = countValue > 0 ? countValue : 0;
                }

                lastFlowOutTime = lastFlowOutTime.AddMilliseconds(pastOutflowUnitQuantity * outflowUnitMilliseconds);
                pastMilliseconds = (currentTime - lastFlowOutTime).TotalMilliseconds;

                counter.LastFlowOutTime = lastFlowOutTime;
                cacheItem.ExpireTime = lastFlowOutTime.Add(currentRule.MaxDrainTime);
            }

            // If the number of requests in the current time window is less than the outflow rate,
            // the request passes directly without waiting.
            countValue = countValue + amount;
            if (countValue <= currentRule.OutflowQuantityPerUnit)
            {
                counter.Value = countValue;
                return (false, countValue, 0L, counter.LastFlowOutTime.Add(currentRule.OutflowUnit));
            }

            // Trigger rate limiting
            // No need to update counter.Value
            if (countValue > currentRule.LimitNumber)
            {
                countValue = countValue - amount;
                return (true, countValue, -1L, counter.LastFlowOutTime.Add(currentRule.OutflowUnit));
            }

            counter.Value = countValue;

            // The requests in the leaky bucket will be processed after one or more time windows.
            long wait = CalculateWaitTime(currentRule.OutflowQuantityPerUnit, outflowUnitMilliseconds, pastMilliseconds, countValue);
            return (false, countValue, wait, counter.LastFlowOutTime.Add(currentRule.OutflowUnit));
        }

        private CounterDictionaryItem<LeakyBucketCounter> AddNewBucket(string target, long amount, LeakyBucketRule currentRule, DateTimeOffset currentTime)
        {
            var startTime = AlgorithmStartTime.ToSpecifiedTypeTime(currentTime, currentRule.OutflowUnit, currentRule.StartTimeType);
            var counter = new LeakyBucketCounter()
            {
                Value = amount,
                LastFlowOutTime = startTime,
            };
            DateTimeOffset expireTime = startTime.Add(currentRule.MaxDrainTime);
            var cacheItem = new CounterDictionaryItem<LeakyBucketCounter>(target, counter)
            {
                ExpireTime = expireTime
            };
            _leakyBuckets.Set(target, cacheItem);
            return cacheItem;
        }

        private static long CalculateWaitTime(long outflowQuantityPerUnit, long outflowUnit, double pastTimeMilliseconds, long countValue)
        {
            long wait = 0;

            var batchNumber = (int)Math.Ceiling(countValue / (double)outflowQuantityPerUnit) - 1;
            if (batchNumber == 1)
            {
                wait = (long)(outflowUnit - pastTimeMilliseconds);
            }
            else
            {
                wait = (long)(outflowUnit * (batchNumber - 1) + (outflowUnit - pastTimeMilliseconds));
            }

            return wait;
        }
    }
}