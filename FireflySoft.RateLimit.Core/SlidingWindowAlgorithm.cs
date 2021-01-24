using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// Sliding Window Algorithm
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    public class SlidingWindowAlgorithm<TRequest> : IRateLimitAlgorithm<TRequest>
    {
        IEnumerable<SlidingWindowRateLimitRule<TRequest>> _rules;

        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="rules"></param>
        public SlidingWindowAlgorithm(IEnumerable<SlidingWindowRateLimitRule<TRequest>> rules)
        {
            _rules = rules;
        }

        /// <summary>
        /// Check the request and return the rate limit result
        /// </summary>
        /// <param name="request"></param>
        /// <param name="storage"></param>
        /// <returns></returns>
        public List<RateLimitCheckResult<TRequest>> Check(TRequest request, IRateLimitStorage storage)
        {
            List<RateLimitCheckResult<TRequest>> results = new List<RateLimitCheckResult<TRequest>>();

            foreach (var rule in _rules)
            {
                if (rule.CheckRuleMatching(request))
                {
                    var target = rule.ExtractTarget(request);
                    if (string.IsNullOrWhiteSpace(target))
                    {
                        throw new NotSupportedException("不支持Target为空");
                    }

                    bool result = CheckSingleRule(target, storage, rule);
                    results.Add(new RateLimitCheckResult<TRequest>()
                    {
                        Rule = rule,
                        Target=target,
                        IsLimit = result
                    });
                }
            }

            return results;
        }

        private bool CheckSingleRule(string target, IRateLimitStorage storage, SlidingWindowRateLimitRule<TRequest> rule)
        {
            if (storage.CheckLocked(target))
            {
                return true;
            }

            var expireTimeSpan = rule.StatWindow;
            var startTime = storage.GetOrAdd($"swst_{target}", new Lazy<long>(() => { return storage.GetCurrentTime(); }));
            var statPeriodArray = rule.GetStatWindowPeriodArray(startTime);
            var currentPeriod = statPeriodArray[0];
            //Debug.WriteLine("currentPeriod:" + currentPeriod);
            storage.SimpleIncrement(currentPeriod, 1, expireTimeSpan);
            var totalAmount = storage.Sum(statPeriodArray);
            //Debug.WriteLine("totalAmount:" + totalAmount);
            if (totalAmount > rule.LimitNumber)
            {
                if (rule.LockSeconds > 0)
                {
                    storage.TryLock(target, TimeSpan.FromSeconds(rule.LockSeconds));
                }

                return true;
            }

            return false;
        }
    }
}
