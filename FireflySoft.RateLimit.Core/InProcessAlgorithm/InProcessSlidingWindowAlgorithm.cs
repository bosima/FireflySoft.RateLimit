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
        readonly CounterDictionary<MemorySlidingWindow> _slidingWindows;

        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="rules">The rate limit rules</param>
        /// <param name="timeProvider">The time provider</param>
        /// <param name="updatable">If rules can be updated</param>
        public InProcessSlidingWindowAlgorithm(IEnumerable<SlidingWindowRule> rules, ITimeProvider timeProvider = null, bool updatable = false)
        : base(rules, timeProvider, updatable)
        {
            _slidingWindows = new CounterDictionary<MemorySlidingWindow>(_timeProvider);
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
                IsLimit = result.IsLimit,
                Target = target,
                Count = result.Count,
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

        private (bool IsLimit, long Count, DateTimeOffset ResetTime) InnerCheckSingleRule(string target, int amount, SlidingWindowRule currentRule)
        {
            bool locked = CheckLocked(target, out DateTimeOffset? expireTime);
            if (locked)
            {
                return (true, -1L, expireTime.Value);
            }

            // get current time
            var currentTime = _timeProvider.GetCurrentLocalTime();
            var currentMilliseconds = currentTime.ToUnixTimeMilliseconds();

            lock (target)
            {
                // gets or sets the sliding window for current target
                MemorySlidingWindow slidingWindow;
                CounterDictionaryItem<MemorySlidingWindow> slidingWindowItem;
                if (!_slidingWindows.TryGet(target, out slidingWindowItem))
                {
                    slidingWindow = new MemorySlidingWindow(currentRule);
                    slidingWindowItem = new CounterDictionaryItem<MemorySlidingWindow>(
                        target,
                        slidingWindow)
                    {
                        ExpireTime = currentTime.AddMilliseconds(currentRule.StatWindow.TotalMilliseconds * 2)
                    };

                    _slidingWindows.Set(target, slidingWindowItem);
                }
                else
                {
                    slidingWindow = slidingWindowItem.Counter;
                }

                // renewal the window
                if (slidingWindowItem.ExpireTime < currentTime.Add(currentRule.StatWindow))
                {
                    slidingWindowItem.ExpireTime = currentTime.AddMilliseconds(currentRule.StatWindow.TotalMilliseconds * 2);
                }

                // rule changed, reset the counter
                slidingWindow.ResetIfRuleChanged(currentRule);

                // maybe replace a period, so call it first
                var period = slidingWindow.LoadPeriod(currentMilliseconds);
                var periodIndex = period.periodIndex;
                var periodId = period.periodId;

                // compare the count and the threshold
                var currentTotalAmount = slidingWindow.GetCount();
                var totalAmount = currentTotalAmount + amount;
                if (currentRule.LimitNumber >= 0 && totalAmount > currentRule.LimitNumber)
                {
                    if (currentRule.LockSeconds > 0)
                    {
                        expireTime = currentTime.AddSeconds(currentRule.LockSeconds);
                        TryLock(target, expireTime.Value);
                        return (true, currentTotalAmount, expireTime.Value);
                    }
                    else
                    {
                        return (true, currentTotalAmount, DateTimeOffset.FromUnixTimeMilliseconds(periodId + 1));
                    }
                }

                // increment the count value
                slidingWindow.IncreamentPeriod(periodIndex, amount);

                return (false, totalAmount, DateTimeOffset.FromUnixTimeMilliseconds(periodId + 1));
            }
        }
    }
}