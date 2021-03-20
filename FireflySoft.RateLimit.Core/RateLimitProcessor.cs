using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// The processor of rate limit
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    public class RateLimitProcessor<TRequest>
    {
        private readonly IRateLimitStorage _storage;
        private readonly IRateLimitAlgorithm<TRequest> _algorithm;
        private readonly ITimeProvider _timeProvider;
        private readonly RateLimitError _error;

        /// <summary>
        /// create a new instance
        /// </summary>
        /// <param name="algorithm"></param>
        /// <param name="storage"></param>
        /// <param name="timeProvider"></param>
        /// <param name="error"></param>
        private RateLimitProcessor(IRateLimitAlgorithm<TRequest> algorithm, IRateLimitStorage storage, ITimeProvider timeProvider, RateLimitError error)
        {
            _algorithm = algorithm;
            _storage = storage;
            _timeProvider = timeProvider;
            _error = error;
            _storage.SetTimeProvider(_timeProvider); // just interim
        }

        /// <summary>
        /// Check the request and return the rate limit result
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public RateLimitResponse<TRequest> Check(TRequest request)
        {
            var results = _algorithm.Check(request, _storage, _timeProvider);
            return BuildResponse(results);
        }

        /// <summary>
        /// Check the request and return the rate limit result
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<RateLimitResponse<TRequest>> CheckAsync(TRequest request)
        {
            var results = await _algorithm.CheckAsync(request, _storage, _timeProvider);
            return BuildResponse(results);
        }

        private RateLimitResponse<TRequest> BuildResponse(List<RateLimitCheckResult<TRequest>> results)
        {
            var response = new RateLimitResponse<TRequest>();
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                if (result.IsLimit)
                {
                    response.Target = result.Target;
                    response.Rule = result.Rule;
                    response.IsLimit = true;
                    response.Error = _error;
                    break;
                }
            }
            return response;
        }

        /// <summary>
        /// The Builder of RateLimitProcessor
        /// </summary>
        public sealed class Builder
        {
            private IRateLimitStorage _storage;
            private IRateLimitAlgorithm<TRequest> _algorithm;
            private ITimeProvider _timeProvider;
            private RateLimitError _error;

            /// <summary>
            /// Sets a instance of IRateLimitStorage
            /// </summary>
            /// <param name="storage"></param>
            /// <returns></returns>
            public Builder WithStorage(IRateLimitStorage storage)
            {
                _storage = storage;
                return this;
            }

            /// <summary>
            /// Sets a instance of IRateLimitAlgorithm
            /// </summary>
            /// <param name="algorithm"></param>
            /// <returns></returns>
            public Builder WithAlgorithm(IRateLimitAlgorithm<TRequest> algorithm)
            {
                _algorithm = algorithm;
                return this;
            }

            /// <summary>
            /// Sets a instance of ITimeProvider
            /// </summary>
            /// <param name="timeProvider"></param>
            /// <returns></returns>
            public Builder WithTimeProvider(ITimeProvider timeProvider)
            {
                _timeProvider = timeProvider;
                return this;
            }

            /// <summary>
            /// Sets a instance of RateLimitError
            /// </summary>
            /// <param name="error"></param>
            /// <returns></returns>
            public Builder WithError(RateLimitError error)
            {
                _error = error;
                return this;
            }

            /// <summary>
            /// Build a instance of RateLimitProcessor with some configurations
            /// </summary>
            /// <returns></returns>
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

                if (_timeProvider == null)
                {
                    _timeProvider = new LocalTimeProvider();
                }

                if (_error == null)
                {
                    _error = new RateLimitError();
                }

                return new RateLimitProcessor<TRequest>(_algorithm, _storage, _timeProvider, _error);
            }
        }
    }
}
