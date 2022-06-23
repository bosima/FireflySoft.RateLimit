using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FireflySoft.RateLimit.Core.Rule;
using FireflySoft.RateLimit.Core.Time;

namespace FireflySoft.RateLimit.Core.InProcessAlgorithm
{
    /// <summary>
    /// Define an in-process token bucket algorithm
    /// </summary>
    public class InProcessTokenBucketAlgorithm : BaseInProcessAlgorithm
    {
        readonly CounterDictionary<TokenBucketCounter> _tokenBuckets;

        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="rules">The rate limit rules</param>
        /// <param name="timeProvider">The time provider</param>
        /// <param name="updatable">If rules can be updated</param>
        public InProcessTokenBucketAlgorithm(IEnumerable<TokenBucketRule> rules, ITimeProvider timeProvider = null, bool updatable = false)
        : base(rules, timeProvider, updatable)
        {
            _tokenBuckets = new CounterDictionary<TokenBucketCounter>(_timeProvider);
        }

        /// <summary>
        /// check single rule for target
        /// </summary>
        /// <param name="target"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        protected override RuleCheckResult CheckSingleRule(string target, RateLimitRule rule)
        {
            var currentRule = rule as TokenBucketRule;
            var amount = 1;

            var result = InnerCheckSingleRule(target, amount, currentRule);
            return new RuleCheckResult()
            {
                IsLimit = result.IsLimit,
                Target = target,
                Count = result.Remaining,
                Rule = rule,
                ResetTime = result.ResetTime,
            };
        }

        /// <summary>
        /// check single rule for target
        /// </summary>
        /// <param name="target"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        protected override async Task<RuleCheckResult> CheckSingleRuleAsync(string target, RateLimitRule rule)
        {
            return await Task.FromResult(CheckSingleRule(target, rule)).ConfigureAwait(false);
        }

        /// <summary>
        /// Decrease the count value of the rate limit target for token bucket algorithm.
        /// </summary>
        /// <param name="target">The target</param>
        /// <param name="amount">The amount of decrease</param>
        /// <param name="currentRule">The rate limit rule</param>
        /// <returns>Amount of token in the bucket</returns>
        public (bool IsLimit, long Remaining, DateTimeOffset ResetTime) InnerCheckSingleRule(string target, long amount, TokenBucketRule currentRule)
        {
            bool locked = CheckLocked(target, out DateTimeOffset? expireTime);
            if (locked)
            {
                return (true, -1, expireTime.Value);
            }

            var currentTime = _timeProvider.GetCurrentLocalTime();
            (bool IsLimit, long Remaining, DateTimeOffset ExpireTime) countResult;

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
                    expireTime = currentTime.AddSeconds(currentRule.LockSeconds);
                    TryLock(target, expireTime.Value);
                    return (true, countResult.Remaining, expireTime.Value);
                }
            }

            return countResult;
        }

        private (bool IsLimit, long Remaining, DateTimeOffset ExpireTime) Count(string target, long amount, TokenBucketRule currentRule, DateTimeOffset currentTime)
        {
            long bucketAmount = 0;

            if (!_tokenBuckets.TryGet(target, out var cacheItem))
            {
                // In the initial state, the bucket is full
                bucketAmount = currentRule.Capacity - amount;
                cacheItem = AddNewBucket(target, bucketAmount, currentRule, currentTime);
                return (false, bucketAmount, cacheItem.Counter.LastInflowTime.Add(currentRule.InflowUnit));
            }

            var counter = (TokenBucketCounter)cacheItem.Counter;

            // If the capacity is reduced to less than the number of remaining tokens,
            // the tokens that cannot be placed in the bucket are removed.
            // But after the capacity increases, the number of tokens in the bucket will not increase directly,
            // which will gradually increase with the inflow.
            if (currentRule.Capacity < counter.Value)
            {
                counter.Value = currentRule.Capacity;
            }

            var inflowUnitMilliseconds = currentRule.InflowUnit.TotalMilliseconds;
            var lastInflowTime = counter.LastInflowTime;
            var pastMilliseconds = (currentTime - lastInflowTime).TotalMilliseconds;
            if (pastMilliseconds < inflowUnitMilliseconds)
            {
                // In the same time window as the previous request, only the token is taken from the bucket
                bucketAmount = counter.Value - amount;
            }
            else
            {
                // After one or more time windows, some tokens need to be put into the bucket,
                // and the number of tokens in the bucket needs to be recalculated.
                var pastInflowUnits = (int)(pastMilliseconds / inflowUnitMilliseconds);
                lastInflowTime = lastInflowTime.AddMilliseconds(pastInflowUnits * inflowUnitMilliseconds);
                var pastInflowQuantity = currentRule.InflowQuantityPerUnit * pastInflowUnits;
                bucketAmount = (counter.Value < 0 ? 0 : counter.Value) + pastInflowQuantity - amount;

                counter.LastInflowTime = lastInflowTime;
                cacheItem.ExpireTime = lastInflowTime.Add(currentRule.MinFillTime);
            }

            // Trigger rate limiting
            if (bucketAmount < 0)
            {
                return (true, 0, cacheItem.Counter.LastInflowTime.Add(currentRule.InflowUnit));
            }

            // Token bucket full
            if (bucketAmount >= currentRule.Capacity)
            {
                bucketAmount = currentRule.Capacity - amount;
            }
            counter.Value = bucketAmount;

            return (false, counter.Value, cacheItem.Counter.LastInflowTime.Add(currentRule.InflowUnit));
        }

        private CounterDictionaryItem<TokenBucketCounter> AddNewBucket(string target, long amount, TokenBucketRule currentRule, DateTimeOffset currentTime)
        {
            var startTime = AlgorithmStartTime.ToSpecifiedTypeTime(currentTime, currentRule.InflowUnit, currentRule.StartTimeType);
            var counter = new TokenBucketCounter()
            {
                Value = amount,
                LastInflowTime = startTime,
            };
            var cacheItem = new CounterDictionaryItem<TokenBucketCounter>(target, counter)
            {
                ExpireTime = startTime.Add(currentRule.MinFillTime)
            };
            _tokenBuckets.Set(target, cacheItem);
            return cacheItem;
        }
    }
}