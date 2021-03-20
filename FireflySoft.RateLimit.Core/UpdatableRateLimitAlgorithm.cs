using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// Updatable Rate Limit Algorithm
    /// </summary>
    public abstract class UpdatableRateLimitAlgorithm<TRequest> : IUpdatableRateLimitAlgorithm<TRequest>
    {
        IEnumerable<RateLimitRule<TRequest>> _rules;
        bool _updatable = false;
        readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        /// <summary>
        /// create a new instance
        /// </summary>
        /// <param name="rules">rate limit rules</param>
        /// <param name="updatable">if rules can be updated</param>
        public UpdatableRateLimitAlgorithm(IEnumerable<RateLimitRule<TRequest>> rules, bool updatable = false)
        {
            _rules = rules;
            _updatable = updatable;

            if (_rules.Any(d => string.IsNullOrWhiteSpace(d.Id)))
            {
                throw new ArgumentNullException("Empty rule ID in collection.");
            }

            if (_rules.GroupBy(d => d.Id).Any(g => g.Count() > 1))
            {
                throw new ArgumentNullException("Duplicate rule ID in collection.");
            }
        }

        /// <summary>
        /// Check single rule for target
        /// </summary>
        /// <param name="target"></param>
        /// <param name="storage"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        protected abstract bool CheckSingleRule(string target, IRateLimitStorage storage, RateLimitRule<TRequest> rule);

        /// <summary>
        /// Check single rule for target
        /// </summary>
        /// <param name="target"></param>
        /// <param name="storage"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        protected abstract Task<bool> CheckSingleRuleAsync(string target, IRateLimitStorage storage, RateLimitRule<TRequest> rule);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rules"></param>
        public void UpdateRules(IEnumerable<RateLimitRule<TRequest>> rules)
        {
            if (_updatable)
            {
                try
                {
                    _lock.EnterWriteLock();

                    if (!_rules.Equals(rules))
                    {
                        _rules = rules;
                    }
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// check a request for rate limit
        /// </summary>
        /// <param name="request">a request</param>
        /// <param name="storage">a instance of IRateLimitStorage</param>
        /// <param name="timeProvider">a instance of ITimeProvider</param>
        /// <returns>the list of check result</returns>
        public List<RateLimitCheckResult<TRequest>> Check(TRequest request, IRateLimitStorage storage, ITimeProvider timeProvider)
        {
            if (_updatable)
            {
                try
                {
                    _lock.EnterReadLock();
                    return CheckAllRules(request, storage, timeProvider);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
            else
            {
                return CheckAllRules(request, storage, timeProvider);
            }
        }

        /// <summary>
        /// check a request for rate limit
        /// </summary>
        /// <param name="request">a request</param>
        /// <param name="storage">a instance of IRateLimitStorage</param>
        /// <param name="timeProvider">a instance of ITimerProvider</param>
        /// <returns>the list of check result</returns>
        public async Task<List<RateLimitCheckResult<TRequest>>> CheckAsync(TRequest request, IRateLimitStorage storage, ITimeProvider timeProvider)
        {
            if (_updatable)
            {
                try
                {
                    _lock.EnterReadLock();
                    return await CheckAllRulesAsync(request, storage, timeProvider);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
            else
            {
                return await CheckAllRulesAsync(request, storage, timeProvider);
            }
        }

        private List<RateLimitCheckResult<TRequest>> CheckAllRules(TRequest request, IRateLimitStorage storage, ITimeProvider timeProvider)
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

                    target = string.Concat(rule.Id, "-", target); // target for rule
                    bool result = CheckSingleRule(target, storage, rule);
                    results.Add(new RateLimitCheckResult<TRequest>()
                    {
                        Rule = rule,
                        Target = target,
                        IsLimit = result
                    });
                }
            }

            return results;
        }

        private async Task<List<RateLimitCheckResult<TRequest>>> CheckAllRulesAsync(TRequest request, IRateLimitStorage storage, ITimeProvider timeProvider)
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

                    target = string.Concat(rule.Id, "-", target); // target for rule
                    bool result = await CheckSingleRuleAsync(target, storage, rule);
                    results.Add(new RateLimitCheckResult<TRequest>()
                    {
                        Rule = rule,
                        Target = target,
                        IsLimit = result
                    });
                }
            }

            return results;
        }
    }
}

