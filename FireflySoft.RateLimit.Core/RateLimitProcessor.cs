using System;
using System.Collections.Generic;
using System.Linq;

namespace FireflySoft.RateLimit.Core
{
    public class RateLimitProcessor<T>
    {
        private readonly IRateLimitStorage _storage;
        private readonly IRateLimitAlgorithm _algorithm;
        private readonly IEnumerable<RateLimitRule<T>> _rules;
        private readonly RateLimitError _error;

        private RateLimitProcessor(IRateLimitStorage storage, IRateLimitAlgorithm algorithm, IEnumerable<RateLimitRule<T>> rules, RateLimitError error)
        {
            _algorithm = algorithm;
            _storage = storage;
            _rules = rules;
            _error = error;
        }

        /// <summary>
        /// 检查限流
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public RateLimitResponse Check(T request)
        {
            var response = new RateLimitResponse();

            if (!_rules.Any())
            {
                return response;
            }

            bool isLimit = false;

            foreach (var rule in _rules)
            {
                if (rule.CheckRuleMatching(request))
                {
                    var target = rule.ExtractTarget(request);
                    if (string.IsNullOrWhiteSpace(target))
                    {
                        throw new NotSupportedException("不支持Target为空");
                    }

                    bool result = _algorithm.Check(target, _storage, rule);
                    if (result && !isLimit)
                    {
                        response.Rule = rule;
                        response.IsLimit = true;
                        response.Error = _error;

                        isLimit = true;
                    }
                }
            }

            return response;
        }

        public sealed class Builder
        {
            private IRateLimitStorage _storage;
            private IRateLimitAlgorithm _algorithm;
            private IEnumerable<RateLimitRule<T>> _rules;
            private RateLimitError _error;

            public Builder WithStorage(IRateLimitStorage storage)
            {
                _storage = storage;
                return this;
            }

            public Builder WithAlgorithm(IRateLimitAlgorithm algorithm)
            {
                _algorithm = algorithm;
                return this;
            }

            public Builder WithError(RateLimitError error)
            {
                _error = error;
                return this;
            }

            public Builder WithRules(IEnumerable<RateLimitRule<T>> rules)
            {
                _rules = rules;
                return this;
            }

            public RateLimitProcessor<T> Build()
            {
                if (_storage == null)
                {
                    _storage = new InProcessMemoryStorage();
                }

                if (_algorithm == null)
                {
                    _algorithm = new FixedWindowAlgorithm();
                }

                if (_rules == null)
                {
                    _rules = new RateLimitRule<T>[0];
                }

                if (_error == null)
                {
                    _error = new RateLimitError();
                }

                return new RateLimitProcessor<T>(_storage, _algorithm, _rules, _error);
            }
        }
    }
}
