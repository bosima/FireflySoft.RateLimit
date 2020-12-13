using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FireflySoft.RateLimit.Core
{
    public class SlidingWindowAlgorithm<TRequest> : IRateLimitAlgorithm<TRequest>
    {
        IEnumerable<SlidingWindowRateLimitRule<TRequest>> _rules;

        public SlidingWindowAlgorithm(IEnumerable<SlidingWindowRateLimitRule<TRequest>> rules)
        {
            _rules = rules;
        }

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
                        IsLimit = result
                    });
                }
            }

            return results;
        }

        private bool CheckSingleRule(string target, IRateLimitStorage storage, SlidingWindowRateLimitRule<TRequest> rule)
        {
            if (storage.CheckLocking(target))
            {
                return true;
            }

            var expireTimeSpan = rule.StatWindow;
            var statPeriodArray = rule.GetStatWindowPeriodArray();
            var currentPeriod = statPeriodArray[0];
            storage.Increment(currentPeriod, 1, expireTimeSpan);
            var totalAmount = storage.MGet(statPeriodArray);
            //Debug.WriteLine(string.Join(",",statPeriodArray));
            //Debug.WriteLine("totalAmount:"+totalAmount);

            if (totalAmount >= rule.LimitNumber)
            {
                if (rule.LockSeconds > 0)
                {
                    storage.Lock(target, TimeSpan.FromSeconds(rule.LockSeconds));
                }

                return true;
            }

            return false;
        }
    }
}
