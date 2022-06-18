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
        /// Create a new instance
        /// </summary>
        /// <param name="rules">The rate limit rules</param>
        /// <param name="timeProvider">The time provider, it is a instance of LocalTimeProvider by default.</param>
        /// <param name="updatable">If rules can be updated</param>
        public BaseAlgorithm(IEnumerable<RateLimitRule> rules, ITimeProvider timeProvider = null, bool updatable = false)
        {
            CheckRules(rules);
            _rules = rules;
            _updatable = updatable;

            _timeProvider = timeProvider;
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
        /// Take a peek at the result of the last processing of the specified target in the specified rule
        /// </summary>
        /// <param name="target"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        protected virtual RuleCheckResult PeekSingleRule(string target, RateLimitRule rule)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Check single rule for target
        /// </summary>
        /// <param name="target"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        protected abstract Task<RuleCheckResult> CheckSingleRuleAsync(string target, RateLimitRule rule);

        /// <summary>
        /// Take a peek at the result of the last processing of the specified target in the specified rule
        /// </summary>
        /// <param name="target"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        protected virtual Task<RuleCheckResult> PeekSingleRuleAsync(string target, RateLimitRule rule)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Update the current rules
        /// </summary>
        /// <param name="rules"></param>
        public void UpdateRules(IEnumerable<RateLimitRule> rules)
        {
            if (_updatable)
            {
                CheckRules(rules);

                using (var l = _mutex.WriterLock())
                {
                    if (!_rules.Equals(rules))
                    {
                        _rules = rules;
                    }
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
                using (var l = await _mutex.WriterLockAsync().ConfigureAwait(false))
                {
                    if (!_rules.Equals(rules))
                    {
                        _rules = rules;
                    }
                }
            }
        }

        /// <summary>
        /// Take a peek at the result of the last rate limiting processing
        /// </summary>
        /// <param name="target">A specified target</param>
        /// <returns>The last check result</returns>
        public AlgorithmCheckResult Peek(string target)
        {
            if (_updatable)
            {
                using (var l = _mutex.ReaderLock())
                {
                    return InnerPeek(target);
                }
            }

            return InnerPeek(target);
        }

        /// <summary>
        /// Take a peek at the result of the last rate limiting processing
        /// </summary>
        /// <param name="target">A specified target</param>
        /// <returns>The last check result</returns>
        public async Task<AlgorithmCheckResult> PeekAsync(string target)
        {
            if (_updatable)
            {
                using (var l = _mutex.ReaderLock())
                {
                    return await InnerPeekAsync(target).ConfigureAwait(false);
                }
            }

            return await InnerPeekAsync(target).ConfigureAwait(false);
        }

        /// <summary>
        /// Check a request for rate limiting
        /// </summary>
        /// <param name="request">a request</param>
        /// <returns>the stream of check result</returns>
        public AlgorithmCheckResult Check(object request)
        {
            if (_updatable)
            {
                using (var l = _mutex.ReaderLock())
                {
                    return InnerCheck(request);
                }
            }

            return InnerCheck(request);
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
                using (var l = await _mutex.ReaderLockAsync().ConfigureAwait(false))
                {
                    return await InnerCheckAsync(request).ConfigureAwait(false);
                }
            }

            return await InnerCheckAsync(request).ConfigureAwait(false);
        }

        private void CheckRules(IEnumerable<RateLimitRule> rules)
        {
            if (rules.Any(d => string.IsNullOrWhiteSpace(d.Id)))
            {
                throw new ArgumentNullException("Empty rule ID in collection.");
            }

            if (rules.GroupBy(d => d.Id).Any(g => g.Count() > 1))
            {
                throw new ArgumentNullException("Duplicate rule ID in collection.");
            }
        }

        private AlgorithmCheckResult InnerPeek(string target)
        {
            var originalRuleChecks = PeekAllRules(target);
            var ruleCheckResults = new List<RuleCheckResult>();
            foreach (var result in originalRuleChecks)
            {
                ruleCheckResults.Add(result);
            }

            return new AlgorithmCheckResult(ruleCheckResults);
        }

        private async Task<AlgorithmCheckResult> InnerPeekAsync(string target)
        {
            var ruleCheckResults = new List<RuleCheckResult>();
            var originalRuleChecks = new AsyncRulePeekEnumerable(this, _rules, target);
            var enumerator = originalRuleChecks.GetAsyncEnumerator();
            try
            {
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    ruleCheckResults.Add(enumerator.Current);
                }
            }
            finally
            {
                await enumerator.DisposeAsync().ConfigureAwait(false);
            }

            return new AlgorithmCheckResult(ruleCheckResults);
        }

        private AlgorithmCheckResult InnerCheck(object request)
        {
            var originalRuleChecks = CheckAllRules(request);
            var ruleCheckResults = new List<RuleCheckResult>();
            foreach (var result in originalRuleChecks)
            {
                ruleCheckResults.Add(result);
            }

            return new AlgorithmCheckResult(ruleCheckResults);
        }

        private async Task<AlgorithmCheckResult> InnerCheckAsync(object request)
        {
            var ruleCheckResults = new List<RuleCheckResult>();
            var originalRuleChecks = new AsyncRuleCheckEnumerable(this, _rules, request);
            var enumerator = originalRuleChecks.GetAsyncEnumerator();
            try
            {
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    ruleCheckResults.Add(enumerator.Current);
                }
            }
            finally
            {
                await enumerator.DisposeAsync().ConfigureAwait(false);
            }

            return new AlgorithmCheckResult(ruleCheckResults);
        }

        private IEnumerable<RuleCheckResult> PeekAllRules(string target)
        {
            foreach (var rule in _rules)
            {
                if (string.IsNullOrWhiteSpace(target))
                {
                    throw new NotSupportedException("Null target is not supported");
                }

                target = string.Intern($"{rule.Id}-{target}");
                yield return PeekSingleRule(target, rule);
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

                    target = $"{rule.Id}-{target}";
                    target = string.Intern(target);
                    yield return CheckSingleRule(target, rule);
                }
            }
        }

        private async Task<(bool IsMatched, RuleCheckResult Result)> TryCheckSingleRuleAsync(RateLimitRule rule, object request)
        {
            var match = false;
            if (rule.CheckRuleMatchingAsync != null)
            {
                match = await rule.CheckRuleMatchingAsync(request).ConfigureAwait(false);
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
                    target = await rule.ExtractTargetAsync(request).ConfigureAwait(false);
                }
                else
                {
                    target = rule.ExtractTarget(request);
                }

                if (string.IsNullOrWhiteSpace(target))
                {
                    throw new NotSupportedException("Null target is not supported");
                }

                target = $"{rule.Id}-{target}";
                target = string.Intern(target);
                var result = await CheckSingleRuleAsync(target, rule).ConfigureAwait(false);
                return (true, result);
            }

            return (false, default(RuleCheckResult));
        }

        private class AsyncRuleCheckEnumerable : IAsyncEnumerable<RuleCheckResult>, IAsyncEnumerator<RuleCheckResult>
        {
            IEnumerator<RateLimitRule> _rules;
            object _request;
            RuleCheckResult _current;
            BaseAlgorithm _baseAlgorithm;

            public AsyncRuleCheckEnumerable(BaseAlgorithm baseAlgorithm, IEnumerable<RateLimitRule> rules, object request)
            {
                _baseAlgorithm = baseAlgorithm;
                _rules = rules.GetEnumerator();
                _request = request;
            }

            public IAsyncEnumerator<RuleCheckResult> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                return this;
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                while (true)
                {
                    var haveNext = _rules.MoveNext();
                    if (haveNext)
                    {
                        var rule = _rules.Current;
                        var result = await _baseAlgorithm.TryCheckSingleRuleAsync(rule, _request).ConfigureAwait(false);
                        if (!result.IsMatched)
                        {
                            continue;
                        }
                        _current = result.Result;
                        return true;
                    }

                    return false;
                }
            }

            public RuleCheckResult Current
            {
                get
                {
                    return _current;
                }
            }

            public ValueTask DisposeAsync()
            {
                _rules.Dispose();
                return default(ValueTask);
            }
        }

        private class AsyncRulePeekEnumerable : IAsyncEnumerable<RuleCheckResult>, IAsyncEnumerator<RuleCheckResult>
        {
            IEnumerator<RateLimitRule> _rules;
            string _target;
            RuleCheckResult _current;
            BaseAlgorithm _baseAlgorithm;

            public AsyncRulePeekEnumerable(BaseAlgorithm baseAlgorithm, IEnumerable<RateLimitRule> rules, string target)
            {
                _baseAlgorithm = baseAlgorithm;
                _rules = rules.GetEnumerator();
                _target = target;
            }

            public IAsyncEnumerator<RuleCheckResult> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                return this;
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                while (true)
                {
                    var haveNext = _rules.MoveNext();
                    if (haveNext)
                    {
                        var rule = _rules.Current;
                        var target = string.Intern($"{rule.Id}-{_target}");
                        _current = await _baseAlgorithm.PeekSingleRuleAsync(target, rule).ConfigureAwait(false);
                        return true;
                    }

                    return false;
                }
            }

            public RuleCheckResult Current
            {
                get
                {
                    return _current;
                }
            }

            public ValueTask DisposeAsync()
            {
                _rules.Dispose();
                return default(ValueTask);
            }
        }

    }
}