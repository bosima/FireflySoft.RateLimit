using System;
using FireflySoft.RateLimit.Core;
using Microsoft.Extensions.DependencyInjection;

namespace FireflySoft.RateLimit.AspNetCore
{
    /// <summary>
    /// Rate Limit Middleware Extensions
    /// </summary>
    public static class RateLimitServiceExtensions
    {
        /// <summary>
        /// Add rate limit service
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="algorithm"></param>
        /// <param name="error"></param>
        /// <param name="interceptor"></param>
        /// <returns></returns>
        public static IServiceCollection AddRateLimit(this IServiceCollection builder, IAlgorithm algorithm, HttpErrorResponse error = null, HttpInvokeInterceptor interceptor = null)
        {
            if (algorithm == null)
            {
                throw new ArgumentNullException("The algorithm service is not registered, please use 'AddRateLimit' in 'ConfigureServices' method.");
            }

            if (error == null)
            {
                error = new HttpErrorResponse()
                {
                    HttpStatusCode = 429,
                    BuildHttpContent = (context, checkResult) =>
                    {
                        return "too many requests";
                    }
                };
            }

            builder.AddSingleton<IAlgorithm>(algorithm);
            builder.AddSingleton<HttpErrorResponse>(error);
            builder.AddSingleton<HttpInvokeInterceptor>(interceptor);
            return builder;
        }
    }
}