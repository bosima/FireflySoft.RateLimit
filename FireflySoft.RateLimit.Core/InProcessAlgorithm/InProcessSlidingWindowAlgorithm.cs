using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FireflySoft.RateLimit.Core.Rule;
using FireflySoft.RateLimit.Core.Time;

namespace FireflySoft.RateLimit.Core.InProcessAlgorithm
{
    /// <summary>
    /// Define an in-process sliding window algorithm
    /// </summary>
    public class InProcessSlidingWindowAlgorithm : BaseInProcessAlgorithm
    {
        readonly Dictionary<string, MemorySlidingWindow> _slidingWindows;

        /// <summary>
        /// create a new instance
        /// </summary>
        /// <param name="rules">The rate limit rules</param>
        /// <param name="timeProvider">The time provider</param>
        /// <param name="updatable">If rules can be updated</param>
        public InProcessSlidingWindowAlgorithm(IEnumerable<SlidingWindowRule> rules, ITimeProvider timeProvider = null, bool updatable = false)
        : base(rules, timeProvider, updatable)
        {
            _slidingWindows = new Dictionary<string, MemorySlidingWindow>();
        }

        /// <summary>
        /// Clear sliding windows after update rules, because the instance of 'MemorySlidingWindow' is cached
        /// </summary>
        protected override void ResetAfterUpdateRules()
        {
            _slidingWindows.Clear();
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
            var currentRule = rule as SlidingWindowRule;
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

        private Tuple<bool, long> InnerCheckSingleRule(string target, int amount, SlidingWindowRule currentRule)
        {
            bool locked = CheckLocked(target);
            if (locked)
            {
                return Tuple.Create(true, -1L);
            }

            // get current time
            var currentTime = _timeProvider.GetCurrentLocalTime();
            var currentMilliseconds = currentTime.ToUnixTimeMilliseconds();

            lock (target)
            {
                MemorySlidingWindow slidingWindow;
                if (!_slidingWindows.TryGetValue(target, out slidingWindow))
                {
                    slidingWindow = new MemorySlidingWindow(currentRule);
                    _slidingWindows.Add(target, slidingWindow);
                }

                // maybe create a new period, so call it first, then call 'GetCount'
                var periodIndex = slidingWindow.LoadPeriod(currentMilliseconds);

                var currentTotalAmount = slidingWindow.GetCount();
                var totalAmount = currentTotalAmount + amount;
                if (currentRule.LimitNumber >= 0 && totalAmount > currentRule.LimitNumber)
                {
                    if (currentRule.LockSeconds > 0)
                    {
                        TryLock(target, currentTime, TimeSpan.FromSeconds(currentRule.LockSeconds));
                    }
                    return Tuple.Create(true, currentTotalAmount);
                }

                slidingWindow.IncreamentPeriod(periodIndex, amount);
                return Tuple.Create(false, totalAmount);
            }
        }
    }
}