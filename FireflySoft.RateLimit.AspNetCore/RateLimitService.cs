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
        /// <returns></returns>
        public static IServiceCollection AddRateLimit(this IServiceCollection builder, IAlgorithm algorithm, HttpRateLimitError error = null)
        {
            if (algorithm == null)
            {
                throw new ArgumentNullException("The algorithm service is not registered, please use 'AddRateLimit' in 'ConfigureServices' method.");
            }

            if (error == null)
            {
                error = new HttpRateLimitError()
                {
                    HttpStatusCode = 429,
                    BuildHttpContent = (context, checkResult) =>
                    {
                        return "too many requests";
                    }
                };
            }

            builder.AddSingleton<IAlgorithm>(algorithm);
            builder.AddSingleton<HttpRateLimitError>(error);
            return builder;
        }
    }
}