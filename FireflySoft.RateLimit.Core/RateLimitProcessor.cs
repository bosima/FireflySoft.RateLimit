using System;
using System.Collections.Generic;
using System.Linq;

namespace FireflySoft.RateLimit.Core
{
    public class RateLimitProcessor<TRequest>
    {
        private readonly IRateLimitStorage _storage;
        private readonly IRateLimitAlgorithm<TRequest> _algorithm;
        private readonly RateLimitError _error;

        private RateLimitProcessor(IRateLimitStorage storage, IRateLimitAlgorithm<TRequest> algorithm, RateLimitError error)
        {
            _algorithm = algorithm;
            _storage = storage;
            _error = error;
        }

        /// <summary>
        /// 检查限流
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public RateLimitResponse<TRequest> Check(TRequest request)
        {
            var response = new RateLimitResponse<TRequest>();

            var results = _algorithm.Check(request, _storage);
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                if (result.IsLimit)
                {
                    response.Rule = result.Rule;
                    response.IsLimit = true;
                    response.Error = _error;
                    break;
                }
            }

            return response;
        }

        public sealed class Builder
        {
            private IRateLimitStorage _storage;
            private IRateLimitAlgorithm<TRequest> _algorithm;
            private RateLimitError _error;

            public Builder WithStorage(IRateLimitStorage storage)
            {
                _storage = storage;
                return this;
            }

            public Builder WithAlgorithm(IRateLimitAlgorithm<TRequest> algorithm)
            {
                _algorithm = algorithm;
                return this;
            }

            public Builder WithError(RateLimitError error)
            {
                _error = error;
                return this;
            }

            public RateLimitProcessor<TRequest> Build()
            {
                if (_storage == null)
                {
                    _storage = new InProcessMemoryStorage();
                }

                if (_algorithm == null)
                {
                    throw new ArgumentNullException("the algorithm can not be null.");
                }

                if (_error == null)
                {
                    _error = new RateLimitError();
                }

                return new RateLimitProcessor<TRequest>(_storage, _algorithm, _error);
            }
        }
    }
}
