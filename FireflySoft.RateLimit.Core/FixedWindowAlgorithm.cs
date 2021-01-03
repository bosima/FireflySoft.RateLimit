using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Caching;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// 固定窗口算法
    /// </summary>
    public class FixedWindowAlgorithm<TRequest> : IRateLimitAlgorithm<TRequest>
    {
        IEnumerable<FixedWindowRateLimitRule<TRequest>> _rules;

        public FixedWindowAlgorithm(IEnumerable<FixedWindowRateLimitRule<TRequest>> rules)
        {
            _rules = rules;
        }

        public List<RateLimitCheckResult<TRequest>> Check(TRequest request, IRateLimitStorage storage)
        {
            List<RateLimitCheckResult<TRequest>> results=new List<RateLimitCheckResult<TRequest>>();

            foreach (var rule in _rules)
            {
                if (rule.CheckRuleMatching(request))
                {
                    var target = rule.ExtractTarget(request);
                    if (string.IsNullOrWhiteSpace(target))
                    {
                        throw new NotSupportedException("不支持Target为空");
                    }

                    bool result = CheckSingleRule(target,storage,rule);
                    results.Add(new RateLimitCheckResult<TRequest>(){
                         Rule=rule,
                         IsLimit=result
                    });
                }
            }

            return results;
        }

        private bool CheckSingleRule(string target, IRateLimitStorage storage, FixedWindowRateLimitRule<TRequest> rule)
        {
            if (storage.CheckLocked(target))
            {
                return true;
            }

            var countAmount = 1;
            var expireTimeSpan = rule.StatWindow;
            var totalAmount = storage.Increment(target, countAmount, expireTimeSpan);
            Debug.WriteLine("totalAmount:"+totalAmount);
            
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

