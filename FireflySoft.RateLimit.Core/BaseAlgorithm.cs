using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FireflySoft.RateLimit.Core.Rule;
using FireflySoft.RateLimit.Core.Time;
using Nito.AsyncEx;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// The base class of rate limit algorithm
    /// </summary>
    public abstract class BaseAlgorithm : IAlgorithm
    {
        /// <summary>
        /// The current time provider
        /// </summary>
        protected ITimeProvider _timeProvider;

        private IEnumerable<RateLimitRule> _rules;
        private bool _updatable = false;
        private readonly AsyncReaderWriterLock _mutex = new AsyncReaderWriterLock();

        /// <summary>
        /// create a new instance
        /// </summary>
        /// <param name="rules">The rate limit rules</param>
        /// <param name="timeProvider">The time provider, it is a instance of LocalTimeProvider by default.</param>
        /// <param name="updatable">If rules can be updated</param>
        public BaseAlgorithm(IEnumerable<RateLimitRule> rules, ITimeProvider timeProvider = null, bool updatable = false)
        {
            _rules = rules;
            _timeProvider = timeProvider;
            _updatable = updatable;

            if (_rules.Any(d => string.IsNullOrWhiteSpace(d.Id)))
            {
                throw new ArgumentNullException("Empty rule ID in collection.");
            }

            if (_rules.GroupBy(d => d.Id).Any(g => g.Count() > 1))
            {
                throw new ArgumentNullException("Duplicate rule ID in collection.");
            }

            if (_timeProvider == null)
            {
                _timeProvider = new LocalTimeProvider();
            }
        }

        /// <summary>
        /// Check single rule for target
        /// </summary>
        /// <param name="target"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        protected abstract RuleCheckResult CheckSingleRule(string target, RateLimitRule rule);

        /// <summary>
        /// Check single rule for target
        /// </summary>
        /// <param name="target"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        protected abstract Task<RuleCheckResult> CheckSingleRuleAsync(string target, RateLimitRule rule);

        /// <summary>
        /// Reset something after update rules
        /// </summary>
        protected virtual void ResetAfterUpdateRules()
        {
        }

        /// <summary>
        /// Update the current rules
        /// </summary>
        /// <param name="rules"></param>
        public void UpdateRules(IEnumerable<RateLimitRule> rules)
        {
            if (_updatable)
            {
                using (var l = _mutex.WriterLock())
                {
                    if (!_rules.Equals(rules))
                    {
                        _rules = rules;
                    }

                    ResetAfterUpdateRules();
                }
            }
        }

        /// <summary>
        /// Update the current rules
        /// </summary>
        /// <param name="rules"></param>
        public async Task UpdateRulesAsync(IEnumerable<RateLimitRule> rules)
        {
            if (_updatable)
            {
                using (var l = await _mutex.WriterLockAsync())
                {
                    if (!_rules.Equals(rules))
                    {
                        _rules = rules;
                    }

                    ResetAfterUpdateRules();
                }
            }
        }

        /// <summary>
        /// check a request for rate limit
        /// </summary>
        /// <param name="request">a request</param>
        /// <returns>the stream of check result</returns>
        public AlgorithmCheckResult Check(object request)
        {
            if (_updatable)
            {
                using (var l = _mutex.ReaderLock())
                {
                    var originalRuleChecks = CheckAllRules(request);
                    var ruleCheckResults = new List<RuleCheckResult>();
                    foreach (var result in originalRuleChecks)
                    {
                        ruleCheckResults.Add(result);
                    }

                    return new AlgorithmCheckResult(ruleCheckResults);
                }
            }
            else
            {
                var originalRuleChecks = CheckAllRules(request);
                var ruleCheckResults = new List<RuleCheckResult>();
                foreach (var result in originalRuleChecks)
                {
                    ruleCheckResults.Add(result);
                }

                return new AlgorithmCheckResult(ruleCheckResults);
            }
        }

        /// <summary>
        /// check a request for rate limit
        /// </summary>
        /// <param name="request">a request</param>
        /// <returns>the stream of check result</returns>
        public async Task<AlgorithmCheckResult> CheckAsync(object request)
        {
            if (_updatable)
            {
                using (var l = await _mutex.ReaderLockAsync())
                {
                    var originalRuleChecks = CheckAllRulesAsync(request);
                    var ruleCheckResults = new List<RuleCheckResult>();
                    await foreach (var result in originalRuleChecks)
                    {
                        ruleCheckResults.Add(result);
                    }

                    return new AlgorithmCheckResult(ruleCheckResults);
                }
            }
            else
            {
                var originalRuleChecks = CheckAllRulesAsync(request);
                var ruleCheckResults = new List<RuleCheckResult>();
                await foreach (var result in originalRuleChecks)
                {
                    ruleCheckResults.Add(result);
                }

                return new AlgorithmCheckResult(ruleCheckResults);
            }
        }

        private IEnumerable<RuleCheckResult> CheckAllRules(object request)
        {
            // the control of traversal rules is given to the external access program
            foreach (var rule in _rules)
            {
                if (rule.CheckRuleMatching(request))
                {
                    var target = rule.ExtractTarget(request);
                    if (string.IsNullOrWhiteSpace(target))
                    {
                        throw new NotSupportedException("Null target is not supported");
                    }

                    target = string.Concat(rule.Id, "-", target);
                    target = string.Intern(target);
                    yield return CheckSingleRule(target, rule);
                }
            }
        }

        private async IAsyncEnumerable<RuleCheckResult> CheckAllRulesAsync(object request)
        {
            // the control of traversal rules is given to the external access program
            foreach (var rule in _rules)
            {
                var match = false;
                if (rule.CheckRuleMatchingAsync != null)
                {
                    match = await rule.CheckRuleMatchingAsync(request);
                }
                else
                {
                    match = rule.CheckRuleMatching(request);
                }

                if (match)
                {
                    string target = string.Empty;
                    if (rule.ExtractTargetAsync != null)
                    {
                        target = await rule.ExtractTargetAsync(request);
                    }
                    else
                    {
                        target = rule.ExtractTarget(request);
                    }

                    if (string.IsNullOrWhiteSpace(target))
                    {
                        throw new NotSupportedException("Null target is not supported");
                    }

                    target = string.Concat(rule.Id, "-", target);
                    target = string.Intern(target);
                    yield return await CheckSingleRuleAsync(target, rule);
                }
            }
        }
    }
}