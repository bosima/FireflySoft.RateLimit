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
        readonly CounterDictionary<FixedWindowCounter> _fixedWindows;

        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="rules">The rate limit rules</param>
        /// <param name="timeProvider">The time provider</param>
        /// <param name="updatable">If rules can be updated</param>
        public InProcessFixedWindowAlgorithm(IEnumerable<FixedWindowRule> rules, ITimeProvider timeProvider = null, bool updatable = false)
        : base(rules, timeProvider, updatable)
        {
            _fixedWindows = new CounterDictionary<FixedWindowCounter>(_timeProvider);
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

            var result = InnerPeekSingleRule(target, currentRule);
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
        /// Check single rule for target.
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
            Tuple<bool, long> countResult;
            lock (target)
            {
                countResult = Count(target, amount, currentTime, currentRule);
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

        private Tuple<bool, long> InnerPeekSingleRule(string target, FixedWindowRule currentRule)
        {
            bool locked = CheckLocked(target);
            if (locked)
            {
                return Tuple.Create(true, -1L);
            }

            if (_fixedWindows.TryGet(target, out var cacheItem))
            {
                var countValue = cacheItem.Counter.Value;

                // This result is inaccurate because it may not actually exceed this threshold
                if (currentRule.LimitNumber >= 0 && countValue >= currentRule.LimitNumber)
                {
                    return Tuple.Create(true, countValue);
                }

                return Tuple.Create(false, countValue);
            }

            return Tuple.Create(false, 0L);
        }

        /// <summary>
        /// Count
        /// </summary>
        /// <param name="target"></param>
        /// <param name="amount"></param>
        /// <param name="currentTime"></param>
        /// <param name="currentRule"></param>
        /// <returns></returns>
        private Tuple<bool, long> Count(string target, long amount, DateTimeOffset currentTime, FixedWindowRule currentRule)
        {
            FixedWindowCounter counter;

            if (_fixedWindows.TryGet(target, out var cacheItem))
            {
                counter = (FixedWindowCounter)cacheItem.Counter;

                // rule changed, the statistical time window is narrowed
                if (counter.StatWindow.CompareTo(currentRule.StatWindow) > 0)
                {
                    if (counter.StartTime.Add(currentRule.StatWindow) <= currentTime)
                    {
                        counter = ResetWindow(currentRule, cacheItem, currentTime);
                    }
                    else
                    {
                        counter = ResizeWindow(currentRule, cacheItem, counter);
                    }
                }

                // rule changed, the statistical time window is enlarged
                if (counter.StatWindow.CompareTo(currentRule.StatWindow) < 0)
                {
                    counter = ResizeWindow(currentRule, cacheItem, counter);
                }
            }
            else
            {
                cacheItem = CreateWindow(target, currentTime, currentRule);
                _fixedWindows.Set(target, cacheItem);
                counter = cacheItem.Counter;
            }

            // check rate limiting threshold
            if (currentRule.LimitNumber >= 0 && counter.Value >= currentRule.LimitNumber)
            {
                return Tuple.Create(true, counter.Value);
            }

            // just increment the counter
            counter.Value += amount;

            return Tuple.Create(false, counter.Value);
        }

        private CounterDictionaryItem<FixedWindowCounter> CreateWindow(string target, DateTimeOffset currentTime, FixedWindowRule currentRule)
        {
            var counter = CreateNewCounter(currentTime, currentRule);
            var cacheItem = new CounterDictionaryItem<FixedWindowCounter>(target, counter)
            {
                ExpireTime = counter.StartTime.Add(currentRule.StatWindow)
            };
            return cacheItem;
        }

        private FixedWindowCounter ResetWindow(FixedWindowRule currentRule, CounterDictionaryItem<FixedWindowCounter> cacheItem, DateTimeOffset currentTime)
        {
            FixedWindowCounter counter = CreateNewCounter(currentTime, currentRule);
            cacheItem.Counter = counter;
            cacheItem.ExpireTime = counter.StartTime.Add(currentRule.StatWindow);
            return counter;
        }

        private FixedWindowCounter ResizeWindow(FixedWindowRule currentRule, CounterDictionaryItem<FixedWindowCounter> cacheItem, FixedWindowCounter counter)
        {
            counter = CreateCounter(counter.Value, counter.StartTime, currentRule);
            cacheItem.Counter = counter;
            cacheItem.ExpireTime = counter.StartTime.Add(currentRule.StatWindow);
            return counter;
        }

        private FixedWindowCounter CreateNewCounter(DateTimeOffset currentTime, FixedWindowRule currentRule)
        {
            DateTimeOffset startTime = AlgorithmStartTime.ToSpecifiedTypeTime(currentTime, currentRule.StatWindow, currentRule.StartTimeType);
            return CreateCounter(0, startTime, currentRule);
        }

        private FixedWindowCounter CreateCounter(long amount, DateTimeOffset startTime, FixedWindowRule currentRule)
        {
            return new FixedWindowCounter()
            {
                Value = amount,
                StartTime = startTime,
                StatWindow = currentRule.StatWindow,
            };
        }
    }
}