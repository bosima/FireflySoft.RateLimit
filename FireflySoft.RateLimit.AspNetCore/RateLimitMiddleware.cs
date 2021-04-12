using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FireflySoft.RateLimit.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace FireflySoft.RateLimit.AspNetCore
{
    /// <summary>
    /// Rate Limit Middleware
    /// </summary>
    public class RateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAlgorithm _algorithm;
        private readonly HttpRateLimitError _error;

        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="next"></param>
        /// <param name="algorithm"></param>
        /// <param name="error"></param>
        public RateLimitMiddleware(RequestDelegate next, IAlgorithm algorithm, HttpRateLimitError error)
        {
            _next = next;
            _algorithm = algorithm;
            _error = error;
        }

        /// <summary>
        /// Asynchronous processing of Middleware
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            var checkResult = await _algorithm.CheckAsync(context);

            if (checkResult.IsLimit)
            {
                context.Response.StatusCode = _error.HttpStatusCode;

                Dictionary<string, StringValues> headers = null;
                if (_error.BuildHttpHeadersAsync != null)
                {
                    headers = await _error.BuildHttpHeadersAsync(context, checkResult).ConfigureAwait(false);
                }
                else if (_error.BuildHttpHeaders != null)
                {
                    headers = _error.BuildHttpHeaders(context, checkResult);
                }

                if (headers != null && headers.Count > 0)
                {
                    foreach (var h in headers)
                    {
                        context.Response.Headers.Append(h.Key, h.Value);
                    }
                }

                string content = null;
                if (_error.BuildHttpContentAsync != null)
                {
                    content = await _error.BuildHttpContentAsync(context, checkResult).ConfigureAwait(false);
                }
                else if (_error.BuildHttpContent != null)
                {
                    content = _error.BuildHttpContent(context, checkResult);
                }

                if (!string.IsNullOrWhiteSpace(content))
                {
                    var bodyContent = Encoding.UTF8.GetBytes(content);
                    await context.Response.Body.WriteAsync(bodyContent, 0, bodyContent.Length).ConfigureAwait(false);
                }
                else
                {
                    await context.Response.WriteAsync(string.Empty).ConfigureAwait(false);
                }
            }
            else
            {
                // Simulation leaky bucket algorithm queuing mechanism
                var wait = checkResult.RuleCheckResults.Max(d => d.Wait);
                if (wait > 0)
                {
                    await Task.Delay((int)wait);
                }

                await _next(context);
            }
        }
    }

    /// <summary>
    /// Rate Limit Middleware Extensions
    /// </summary>
    public static class RateLimitMiddlewareExtensions
    {
        /// <summary>
        /// Using rate limit processor
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseRateLimit(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RateLimitMiddleware>();
        }
    }
}
