using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FireflySoft.RateLimit.Core.Rule;
using FireflySoft.RateLimit.Core.Time;

namespace FireflySoft.RateLimit.Core.InProcessAlgorithm
{
    /// <summary>
    /// Define an in-process fixed window algorithm
    /// </summary>
    public class InProcessFixedWindowAlgorithm : BaseInProcessAlgorithm
    {
        /// <summary>
        /// create a new instance
        /// </summary>
        /// <param name="rules">The rate limit rules</param>
        /// <param name="timeProvider">The time provider</param>
        /// <param name="updatable">If rules can be updated</param>
        public InProcessFixedWindowAlgorithm(IEnumerable<FixedWindowRule> rules, ITimeProvider timeProvider = null, bool updatable = false)
        : base(rules, timeProvider, updatable)
        {
        }

        /// <summary>
        /// Take a peek at the result of the last processing of the specified target in the specified rule
        /// </summary>
        /// <param name="target"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        protected override RuleCheckResult PeekSingleRule(string target, RateLimitRule rule)
        {
            var currentRule = rule as FixedWindowRule;
            var amount = 1;

            var result = InnerPeekSingleRule(target, amount, currentRule);
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
        protected override RuleCheckResult CheckSingleRule(string target, RateLimitRule rule)
        {
            var currentRule = rule as FixedWindowRule;
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

        private Tuple<bool, long> InnerCheckSingleRule(string target, int amount, FixedWindowRule currentRule)
        {
            bool locked = CheckLocked(target);
            if (locked)
            {
                return Tuple.Create(true, -1L);
            }

            var currentTime = _timeProvider.GetCurrentLocalTime();
            var startTime = AlgorithmStartTime.ToSpecifiedTypeTime(currentTime, currentRule.StatWindow, currentRule.StartTimeType);

            Tuple<bool, long> incrementResult;
            lock (target)
            {
                DateTimeOffset expireTime = startTime.Add(currentRule.StatWindow);
                incrementResult = SimpleIncrement(target, amount, expireTime, currentRule.LimitNumber);
            }

            var checkResult = incrementResult.Item1;
            if (checkResult)
            {
                if (currentRule.LockSeconds > 0)
                {
                    TryLock(target, currentTime, TimeSpan.FromSeconds(currentRule.LockSeconds));
                    return Tuple.Create(checkResult, incrementResult.Item2);
                }
            }

            return Tuple.Create(checkResult, incrementResult.Item2);
        }

        private Tuple<bool, long> InnerPeekSingleRule(string target, int amount, FixedWindowRule currentRule)
        {
            bool locked = CheckLocked(target);
            if (locked)
            {
                return Tuple.Create(true, -1L);
            }

            var result = _cache.GetCacheItem(target);
            if (result != null)
            {
                // This result is inaccurate because it may not actually exceed this threshold
                var countValue = (long)result.Value;
                if (currentRule.LimitNumber >= 0 && countValue >= currentRule.LimitNumber)
                {
                    return Tuple.Create(true, countValue);
                }
                
                return Tuple.Create(false, countValue);
            }

            return Tuple.Create(false, 0L);
        }

        /// <summary>
        /// Increment a value with expire time and limit value
        /// </summary>
        /// <param name="target"></param>
        /// <param name="amount"></param>
        /// <param name="expireTime"></param>
        /// <param name="checkNumber"></param>
        /// <returns></returns>
        private Tuple<bool, long> SimpleIncrement(string target, long amount, DateTimeOffset expireTime, int checkNumber = -1)
        {
            var result = _cache.GetCacheItem(target);
            if (result != null)
            {
                var countValue = (long)result.Value;
                if (checkNumber >= 0 && countValue >= checkNumber)
                {
                    return Tuple.Create(true, countValue);
                }
                var newCountValue = countValue + amount;
                result.Value = newCountValue;
                return Tuple.Create(false, newCountValue);
            }

            _cache.Add(target, amount, expireTime);
            return Tuple.Create(false, amount);
        }
    }
}