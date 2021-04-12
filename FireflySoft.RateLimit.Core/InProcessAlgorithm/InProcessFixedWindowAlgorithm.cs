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
    }
}