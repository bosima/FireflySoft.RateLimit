using System;
using System.Text;
using System.Threading.Tasks;
using FireflySoft.RateLimit.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FireflySoft.RateLimit.AspNetCore
{
    /// <summary>
    /// 限流处理中间件
    /// </summary>
    public class RateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILoggerFactory _loggerFactory;
        private readonly RateLimitProcessor<HttpContext> _processor;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="next"></param>
        /// <param name="options"></param>
        /// <param name="loggerFactory"></param>
        public RateLimitMiddleware(RequestDelegate next, RateLimitProcessor<HttpContext> processor, ILoggerFactory loggerFactory)
        {
            if (processor == null)
            {
                throw new ArgumentNullException("RateLimitProcessor is null.");
            }

            _next = next;
            _loggerFactory = loggerFactory;
            _processor = processor;
        }

        /// <summary>
        /// 中间件的异步处理
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            var checkResult = _processor.Check(context);

            if (checkResult.IsLimit)
            {
                context.Response.StatusCode = checkResult.Error.Code;

                if (!string.IsNullOrWhiteSpace(checkResult.Error.Message))
                {
                    var bodyContent = Encoding.UTF8.GetBytes(checkResult.Error.Message);
                    await context.Response.Body.WriteAsync(bodyContent, 0, bodyContent.Length).ConfigureAwait(false);
                }
                else
                {
                    await context.Response.WriteAsync(string.Empty).ConfigureAwait(false);
                }
            }
            else
            {
                await _next(context);
            }
        }
    }

    /// <summary>
    /// 限流中间件扩展
    /// </summary>
    public static class RateLimitMiddlewareExtensions
    {
        /// <summary>
        /// 使用限流处理器
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="processor"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseRateLimit(this IApplicationBuilder builder, RateLimitProcessor<HttpContext> processor)
        {
            return builder.UseMiddleware<RateLimitMiddleware>(processor);
        }
    }
}
