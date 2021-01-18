using System;
using System.Collections.Generic;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// Token Bucket Algorithm
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    public class TokenBucketAlgorithm<TRequest> : IRateLimitAlgorithm<TRequest>
    {
        IEnumerable<TokenBucketRateLimitRule<TRequest>> _rules;

        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="rules"></param>
        public TokenBucketAlgorithm(IEnumerable<TokenBucketRateLimitRule<TRequest>> rules)
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
                        IsLimit = result
                    });
                }
            }

            return results;
        }

        private bool CheckSingleRule(string target, IRateLimitStorage storage, TokenBucketRateLimitRule<TRequest> rule)
        {
            if (storage.CheckLocked(target))
            {
                return true;
            }

            var bucketAmount = storage.TokenBucketDecrement(target, 1, rule.Capacity, (int)rule.InflowUnit.TotalMilliseconds, rule.InflowQuantityPerUnit);
            //Debug.WriteLine("bucketAmount" + bucketAmount);

            if (bucketAmount < 0)
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
