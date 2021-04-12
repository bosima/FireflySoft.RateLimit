using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using FireflySoft.RateLimit.Core;
using System.Collections.Generic;
using System;

namespace FireflySoft.RateLimit.AspNet
{
    /// <summary>
    /// 限流处理器
    /// </summary>
    public class RateLimitHandler : DelegatingHandler
    {
        private readonly IAlgorithm _algorithm;
        private readonly HttpRateLimitError _error;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="algorithm"></param>
        /// <param name="error"></param>
        public RateLimitHandler(IAlgorithm algorithm, HttpRateLimitError error = null)
        {
            if (algorithm == null)
            {
                throw new ArgumentNullException("The algorithm is null.");
            }

            if (error == null)
            {
                error = new HttpRateLimitError()
                {
                    HttpStatusCode = 429,
                    BuildHttpContent = (request, checkResult) =>
                    {
                        return "too many requests";
                    }
                };
            }

            _algorithm = algorithm;
            _error = error;
        }

        /// <summary>
        /// 异步发送
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected async override Task<HttpResponseMessage> SendAsync(
               HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var checkResult = _algorithm.Check(request);

            if (checkResult.IsLimit)
            {
                HttpResponseMessage response = new HttpResponseMessage();
                response.StatusCode = (System.Net.HttpStatusCode)_error.HttpStatusCode;

                Dictionary<string, string> headers = null;
                if (_error.BuildHttpHeadersAsync != null)
                {
                    headers = await _error.BuildHttpHeadersAsync(request, checkResult).ConfigureAwait(false);
                }
                else if (_error.BuildHttpHeaders != null)
                {
                    headers = _error.BuildHttpHeaders(request, checkResult);
                }
                if (headers != null && headers.Count > 0)
                {
                    foreach (var h in headers)
                    {
                        response.Headers.Add(h.Key, h.Value);
                    }
                }

                string content = null;
                if (_error.BuildHttpContentAsync != null)
                {
                    content = await _error.BuildHttpContentAsync(request, checkResult).ConfigureAwait(false);
                }
                else if (_error.BuildHttpContent != null)
                {
                    content = _error.BuildHttpContent(request, checkResult);
                }
                if (!string.IsNullOrWhiteSpace(content))
                {
                    response.Content = new StringContent(content);
                }
                else
                {
                    response.Content = new StringContent(string.Empty);
                }

                return response;
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
