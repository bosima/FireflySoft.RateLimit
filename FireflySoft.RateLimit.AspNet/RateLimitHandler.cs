using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using FireflySoft.RateLimit.Core;

namespace FireflySoft.RateLimit.AspNet
{
    /// <summary>
    /// 限流处理器
    /// </summary>
    public class RateLimitHandler : DelegatingHandler
    {
        private readonly RateLimitProcessor<HttpRequestMessage> _processor;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="processor"></param>
        public RateLimitHandler(RateLimitProcessor<HttpRequestMessage> processor)
        {
            _processor = processor;
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
            // HttpContextBase context = (HttpContextBase)request.Properties["MS_HttpContext"];
            var checkResult = _processor.Check(request);

            if (checkResult.IsLimit)
            {
                HttpResponseMessage response = new HttpResponseMessage();
                response.StatusCode = (System.Net.HttpStatusCode)checkResult.Error.Code;

                if (!string.IsNullOrWhiteSpace(checkResult.Error.Message))
                {
                    response.Content = new StringContent(checkResult.Error.Message);
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
