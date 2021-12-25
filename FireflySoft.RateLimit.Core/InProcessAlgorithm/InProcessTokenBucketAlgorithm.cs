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
            var inflowUnit = currentRule.InflowUnit.TotalMilliseconds;

            if (!_tokenBuckets.TryGet(target, out var cacheItem))
            {
                bucketAmount = currentRule.Capacity - amount;
                AddNewCounter(target, bucketAmount, currentRule, currentTime);
                return new Tuple<bool, long>(false, bucketAmount);
            }

            var counter = (TokenBucketCounter)cacheItem.Counter;

            // if rule version less than input rule version, do nothing

            var lastTime = counter.LastFlowInTime;
            var lastTimeChanged = false;
            var pastTime = currentTime - lastTime;
            var pastTimeMilliseconds = pastTime.TotalMilliseconds;
            if (pastTimeMilliseconds < inflowUnit)
            {
                bucketAmount = counter.Value - amount;
            }
            else
            {
                var pastInflowUnitQuantity = (int)(pastTimeMilliseconds / inflowUnit);
                lastTime = lastTime.AddMilliseconds(pastInflowUnitQuantity * inflowUnit);
                lastTimeChanged = true;
                var pastInflowQuantity = currentRule.InflowQuantityPerUnit * pastInflowUnitQuantity;
                bucketAmount = (counter.Value < 0 ? 0 : counter.Value) + pastInflowQuantity - amount;
            }

            if (bucketAmount < 0)
            {
                return new Tuple<bool, long>(true, bucketAmount);
            }

            if (bucketAmount >= currentRule.Capacity)
            {
                bucketAmount = currentRule.Capacity - amount;
            }

            counter.Value = bucketAmount;

            if (lastTimeChanged)
            {
                counter.LastFlowInTime = lastTime;
                cacheItem.ExpireTime = lastTime.Add(currentRule.MinFillTime);
            }

            return new Tuple<bool, long>(false, counter.Value);
        }

        private TokenBucketCounter AddNewCounter(string target, long amount, TokenBucketRule currentRule, DateTimeOffset currentTime)
        {
            var startTime = AlgorithmStartTime.ToSpecifiedTypeTime(currentTime, currentRule.InflowUnit, currentRule.StartTimeType);
            var counter = new TokenBucketCounter()
            {
                Value = amount,
                LastFlowInTime = startTime,
            };
            DateTimeOffset expireTime = startTime.Add(currentRule.MinFillTime);
            _tokenBuckets.Set(target, new CounterDictionaryItem<TokenBucketCounter>(target, counter)
            {
                ExpireTime = expireTime
            });
            return counter;
        }
    }
}