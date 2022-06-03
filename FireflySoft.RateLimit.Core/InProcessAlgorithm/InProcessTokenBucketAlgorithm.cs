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
                IsLimit = result.Item1,
                Target = target,
                Count = result.Item2,
                Rule = rule
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
            return await Task.FromResult(CheckSingleRule(target, rule));
        }

        /// <summary>
        /// Decrease the count value of the rate limit target for token bucket algorithm.
        /// </summary>
        /// <param name="target">The target</param>
        /// <param name="amount">The amount of decrease</param>
        /// <param name="currentRule">The rate limit rule</param>
        /// <returns>Amount of token in the bucket</returns>
        public Tuple<bool, long> InnerCheckSingleRule(string target, long amount, TokenBucketRule currentRule)
        {
            bool locked = CheckLocked(target);
            if (locked)
            {
                return new Tuple<bool, long>(true, -1);
            }

            var currentTime = _timeProvider.GetCurrentLocalTime();
            Tuple<bool, long> countResult;

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

            return Tuple.Create(checkResult, countResult.Item2);
        }

        private Tuple<bool, long> Count(string target, long amount, TokenBucketRule currentRule, DateTimeOffset currentTime)
        {
            long bucketAmount = 0;

            if (!_tokenBuckets.TryGet(target, out var cacheItem))
            {
                // In the initial state, the bucket is full
                bucketAmount = currentRule.Capacity - amount;
                AddNewCounter(target, bucketAmount, currentRule, currentTime);
                return new Tuple<bool, long>(false, bucketAmount);
            }

            var counter = (TokenBucketCounter)cacheItem.Counter;

            // If the rule is changed, the token bucket will not be updated directly.
            // For example:
            // After the capacity increases, the number of tokens in the bucket will not increase directly,
            // which will gradually increase with the inflow.

            var inflowUnitMilliseconds = currentRule.InflowUnit.TotalMilliseconds;
            var lastInflowTime = counter.LastInflowTime;
            var lastTimeChanged = false;
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
                lastTimeChanged = true;
                var pastInflowQuantity = currentRule.InflowQuantityPerUnit * pastInflowUnits;
                bucketAmount = (counter.Value < 0 ? 0 : counter.Value) + pastInflowQuantity - amount;
            }

            // Trigger rate limiting
            if (bucketAmount < 0)
            {
                return new Tuple<bool, long>(true, bucketAmount);
            }

            // Token bucket full
            if (bucketAmount >= currentRule.Capacity)
            {
                bucketAmount = currentRule.Capacity - amount;
            }
            counter.Value = bucketAmount;

            if (lastTimeChanged)
            {
                counter.LastInflowTime = lastInflowTime;
                cacheItem.ExpireTime = lastInflowTime.Add(currentRule.MinFillTime);
            }

            return new Tuple<bool, long>(false, counter.Value);
        }

        private TokenBucketCounter AddNewCounter(string target, long amount, TokenBucketRule currentRule, DateTimeOffset currentTime)
        {
            var startTime = AlgorithmStartTime.ToSpecifiedTypeTime(currentTime, currentRule.InflowUnit, currentRule.StartTimeType);
            var counter = new TokenBucketCounter()
            {
                Value = amount,
                LastInflowTime = startTime,
            };
            _tokenBuckets.Set(target, new CounterDictionaryItem<TokenBucketCounter>(target, counter)
            {
                ExpireTime = startTime.Add(currentRule.MinFillTime)
            });
            return counter;
        }
    }
}