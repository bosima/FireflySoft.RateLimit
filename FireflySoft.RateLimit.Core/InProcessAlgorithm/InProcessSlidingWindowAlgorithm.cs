using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        /// <summary>
        /// create a new instance
        /// </summary>
        /// <param name="rules">The rate limit rules</param>
        /// <param name="timeProvider">The time provider</param>
        /// <param name="updatable">If rules can be updated</param>
        public InProcessSlidingWindowAlgorithm(IEnumerable<SlidingWindowRule> rules, ITimeProvider timeProvider = null, bool updatable = false)
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
                // get current period
                long currentPeriod = GetCurrentPeriod(target, currentRule, currentMilliseconds);

                // get the stat periods
                var statPeriodArray = GetStatWindowPeriodArray(currentPeriod, currentRule);

                // get the total amount of all stat periods
                var currentTotalAmount = Sum(statPeriodArray.Select(d => $"{target}-{d}").ToList());
                var totalAmount = currentTotalAmount + amount;
                //Console.WriteLine("totalAmount:" + totalAmount);
                if (currentRule.LimitNumber >= 0 && totalAmount > currentRule.LimitNumber)
                {
                    if (currentRule.LockSeconds > 0)
                    {
                        TryLock(target, currentTime, TimeSpan.FromSeconds(currentRule.LockSeconds));
                    }
                    return Tuple.Create(true, currentTotalAmount);
                }

                // increment for current period
                var expireTime = currentTime.Add(currentRule.StatWindow).AddSeconds(3); // add 3s for: avoid data loss because other steps take too much time
                var incrementKey = $"{target}-{currentPeriod}";
                SimpleIncrement(incrementKey, amount, expireTime);

                return Tuple.Create(false, totalAmount);
            }
        }

        private long GetCurrentPeriod(string target, SlidingWindowRule currentRule, long currentMilliseconds)
        {
            long statPeriodMilliseconds = (long)currentRule.StatPeriod.TotalMilliseconds;
            long currentPeriod = 0;
            string ltKey = $"{target}-lt";

            var currentPeriodObj = _cache.Get(ltKey);
            if (currentPeriodObj == null)
            {
                var startTimeMilliseconds = AlgorithmStartTime.ToSpecifiedTypeTime(currentMilliseconds, currentRule.StatWindow, currentRule.StartTimeType);
                currentPeriod = startTimeMilliseconds + statPeriodMilliseconds - 1;
                _cache.Add(ltKey, currentPeriod, DateTimeOffset.MaxValue);
            }
            else
            {
                currentPeriod = (long)currentPeriodObj;
                if (currentMilliseconds > currentPeriod)
                {
                    do
                    {
                        currentPeriod += statPeriodMilliseconds;
                    } while (currentMilliseconds > currentPeriod);

                    //_cache.Set(ltKey, currentPeriod, DateTimeOffset.MaxValue);
                }
            }

            return currentPeriod;
        }

        private long[] GetStatWindowPeriodArray(long currentPeriod, SlidingWindowRule currentRule)
        {
            var periodNumber = currentRule.PeriodNumber;
            var statPeriodMilliseconds = (long)currentRule.StatPeriod.TotalMilliseconds;

            long[] periodSet = new long[periodNumber];
            periodSet[0] = currentPeriod;
            var prevPeriod = currentPeriod;
            for (int i = 1; i < periodNumber; i++)
            {
                prevPeriod = prevPeriod - statPeriodMilliseconds;
                periodSet[i] = prevPeriod;
            }

            //Debug.WriteLine(string.Join(",", periodSet.Select(d => d.ToString())));
            return periodSet;
        }

        /// <summary>
        /// Gets the sum of the counts of multiple rate limit targets
        /// </summary>
        /// <param name="targets">The targets</param>
        /// <returns></returns>
        private long Sum(IEnumerable<string> targets)
        {
            var values = _cache.GetValues(targets);
            if (values != null && values.Count > 0)
            {
                //Debug.WriteLine(string.Join(",", values.Select(d => d.Key + ":" + (d.Value as CountValue).Value.ToString())));

                long t = 0;
                foreach (var key in values.Keys)
                {
                    if (values.TryGetValue(key, out object v))
                    {
                        if (v != null)
                        {
                            t += (v as CountValue).Value;
                        }
                    }
                }
                return t;
            }

            return 0;
        }
    }
}