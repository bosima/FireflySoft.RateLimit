using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FireflySoft.RateLimit.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using FireflySoft.RateLimit.Core.Rule;

namespace FireflySoft.RateLimit.AspNetCore
{
    /// <summary>
    /// Rate Limit Middleware
    /// </summary>
    public class RateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAlgorithm _algorithm;
        private readonly HttpErrorResponse _error;
        private readonly HttpInvokeInterceptor _interceptor;

        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="next"></param>
        /// <param name="algorithm"></param>
        /// <param name="error"></param>
        /// <param name="interceptor"></param>
        public RateLimitMiddleware(RequestDelegate next, IAlgorithm algorithm, HttpErrorResponse error, HttpInvokeInterceptor interceptor)
        {
            _next = next;
            _algorithm = algorithm;
            _error = error;
            _interceptor = interceptor;
        }

        /// <summary>
        /// Asynchronous processing of Middleware
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            await DoOnBeforeCheck(context, _algorithm).ConfigureAwait(false);
            var checkResult = await _algorithm.CheckAsync(context);
            SetRateLimitResultHeader(context, checkResult);
            await DoOnAfterCheck(context, checkResult).ConfigureAwait(false);

            if (checkResult.IsLimit)
            {
                await DoOnTriggered(context, checkResult).ConfigureAwait(false);

                context.Response.StatusCode = _error.HttpStatusCode;

                var headers = await BuildHttpHeaders(context, checkResult).ConfigureAwait(false);
                if (headers != null && headers.Count > 0)
                {
                    foreach (var h in headers)
                    {
                        context.Response.Headers.Append(h.Key, h.Value);
                    }
                }

                string content = await BuildHttpContent(context, checkResult).ConfigureAwait(false);
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
                await DoOnBreforUntriggeredDoNext(context, checkResult).ConfigureAwait(false);

                if (checkResult.RuleCheckResults.Any())
                {
                    await DoLeakyBucketWait(checkResult).ConfigureAwait(false);
                }

                await _next(context);

                await DoOnAfterUntriggeredDoNext(context, checkResult).ConfigureAwait(false);
            }
        }

        private static async Task DoLeakyBucketWait(AlgorithmCheckResult checkResult)
        {
            // Simulation leaky bucket algorithm queuing mechanism
            var wait = checkResult.RuleCheckResults.Max(d => d.Wait);
            if (wait > 0)
            {
                await Task.Delay((int)wait).ConfigureAwait(false);
            }
        }

        private async Task<string> BuildHttpContent(HttpContext context, AlgorithmCheckResult checkResult)
        {
            string content = null;
            if (_error.BuildHttpContentAsync != null)
            {
                content = await _error.BuildHttpContentAsync(context, checkResult).ConfigureAwait(false);
            }
            else if (_error.BuildHttpContent != null)
            {
                content = _error.BuildHttpContent(context, checkResult);
            }

            return content;
        }

        private async Task<Dictionary<string, StringValues>> BuildHttpHeaders(HttpContext context, AlgorithmCheckResult checkResult)
        {
            Dictionary<string, StringValues> headers = null;
            if (_error.BuildHttpHeadersAsync != null)
            {
                headers = await _error.BuildHttpHeadersAsync(context, checkResult).ConfigureAwait(false);
            }
            else if (_error.BuildHttpHeaders != null)
            {
                headers = _error.BuildHttpHeaders(context, checkResult);
            }

            return headers;
        }

        private async Task DoOnTriggered(HttpContext context, AlgorithmCheckResult checkResult)
        {
            if (_interceptor != null)
            {
                if (_interceptor.OnTriggeredAsync != null)
                {
                    await _interceptor.OnTriggeredAsync(context, checkResult).ConfigureAwait(false);
                }
                else if (_interceptor.OnTriggered != null)
                {
                    _interceptor.OnTriggered(context, checkResult);
                }
            }
        }

        private async Task DoOnBeforeCheck(HttpContext context, IAlgorithm algorithm)
        {
            if (_interceptor != null)
            {
                if (_interceptor.OnBeforeCheckAsync != null)
                {
                    await _interceptor.OnBeforeCheckAsync(context, algorithm).ConfigureAwait(false);
                }
                else if (_interceptor.OnBeforeCheck != null)
                {
                    _interceptor.OnBeforeCheck(context, algorithm);
                }
            }
        }

        private async Task DoOnAfterCheck(HttpContext context, AlgorithmCheckResult checkResult)
        {
            if (_interceptor != null)
            {
                if (_interceptor.OnAfterCheckAsync != null)
                {
                    await _interceptor.OnAfterCheckAsync(context, checkResult).ConfigureAwait(false);
                }
                else if (_interceptor.OnAfterCheck != null)
                {
                    _interceptor.OnAfterCheck(context, checkResult);
                }
            }
        }

        private void SetRateLimitResultHeader(HttpContext context, AlgorithmCheckResult checkResult)
        {
            foreach (var result in checkResult.RuleCheckResults)
            {
                var threshold = result.Rule.GetLimitThreshold();
                context.Response.Headers.AppendCommaSeparatedValues("X-RateLimit-Limit", threshold.ToString());
                context.Response.Headers.AppendCommaSeparatedValues("X-RateLimit-Remaining", (threshold - result.Count).ToString());
                context.Response.Headers.AppendCommaSeparatedValues("X-RateLimit-Reset", result.ResetTime.ToUnixTimeSeconds().ToString());

                if (checkResult.IsLimit)
                {
                    context.Response.Headers.AppendCommaSeparatedValues("Retry-After", result.ResetTime.Subtract(DateTimeOffset.Now).TotalSeconds.ToString());
                }
            }
        }

        private async Task DoOnBreforUntriggeredDoNext(HttpContext context, AlgorithmCheckResult checkResult)
        {
            if (_interceptor != null)
            {
                if (_interceptor.OnBreforUntriggeredDoNextAsync != null)
                {
                    await _interceptor.OnBreforUntriggeredDoNextAsync(context, checkResult).ConfigureAwait(false);
                }
                else if (_interceptor.OnBreforUntriggeredDoNext != null)
                {
                    _interceptor.OnBreforUntriggeredDoNext(context, checkResult);
                }
            }
        }

        private async Task DoOnAfterUntriggeredDoNext(HttpContext context, AlgorithmCheckResult checkResult)
        {
            if (_interceptor != null)
            {
                if (_interceptor.OnAfterUntriggeredDoNextAsync != null)
                {
                    await _interceptor.OnAfterUntriggeredDoNextAsync(context, checkResult).ConfigureAwait(false);
                }
                else if (_interceptor.OnAfterUntriggeredDoNext != null)
                {
                    _interceptor.OnAfterUntriggeredDoNext(context, checkResult);
                }
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
