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
        /// Take a peek at the result of the last processing of the specified target in the specified rule
        /// </summary>
        /// <param name="target"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        protected override RuleCheckResult PeekSingleRule(string target, RateLimitRule rule)
        {
            throw new NotSupportedException();
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
                IsLimit = result.Item1,
                Target = target,
                Count = result.Item2,
                Rule = rule,
                Wait = result.Item3,
                ResetTime = result.Item4,
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
        public Tuple<bool, long, long, DateTimeOffset> InnerCheckSingleRule(string target, long amount, LeakyBucketRule currentRule)
        {
            bool locked = CheckLocked(target, out DateTimeOffset? expireTime);
            if (locked)
            {
                return Tuple.Create(true, -1L, -1L, expireTime.Value);
            }

            var currentTime = _timeProvider.GetCurrentLocalTime();
            Tuple<bool, long, long, DateTimeOffset> countResult;

            lock (target)
            {
                countResult = Count(target, amount, currentRule, currentTime);
            }

            // do free lock
            var checkResult = countResult.Item1;
            if (checkResult)
            {
                if (currentRule.LockSeconds > 0)
                {
                    TryLock(target, currentTime, TimeSpan.FromSeconds(currentRule.LockSeconds));
                }
            }

            return Tuple.Create(checkResult, countResult.Item2, countResult.Item3, countResult.Item4);
        }

        private Tuple<bool, long, long, DateTimeOffset> Count(string target, long amount, LeakyBucketRule currentRule, DateTimeOffset currentTime)
        {
            if (!_leakyBuckets.TryGet(target, out var cacheItem))
            {
                cacheItem = AddNewBucket(target, amount, currentRule, currentTime);
                return Tuple.Create(false, amount, 0L, cacheItem.Counter.LastFlowOutTime.Add(currentRule.OutflowUnit));
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
                return Tuple.Create(false, countValue, 0L, counter.LastFlowOutTime.Add(currentRule.OutflowUnit));
            }

            // Trigger rate limiting
            // No need to update counter.Value
            if (countValue > currentRule.LimitNumber)
            {
                countValue = countValue - amount;
                return Tuple.Create(true, countValue, -1L, counter.LastFlowOutTime.Add(currentRule.OutflowUnit));
            }

            counter.Value = countValue;

            // The requests in the leaky bucket will be processed after one or more time windows.
            long wait = CalculateWaitTime(currentRule.OutflowQuantityPerUnit, outflowUnitMilliseconds, pastMilliseconds, countValue);
            return Tuple.Create(false, countValue, wait, counter.LastFlowOutTime.Add(currentRule.OutflowUnit));
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