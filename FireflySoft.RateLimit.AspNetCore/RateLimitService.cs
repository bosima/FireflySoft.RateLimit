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
        /// <param name="services"></param>
        /// <param name="algorithm"></param>
        /// <param name="error"></param>
        /// <param name="interceptor"></param>
        /// <returns></returns>
        public static IServiceCollection AddRateLimit(this IServiceCollection services, IAlgorithm algorithm, HttpErrorResponse error = null, HttpInvokeInterceptor interceptor = null)
        {
            if (algorithm == null)
            {
                throw new ArgumentNullException("The algorithm service is not registered, please use 'AddRateLimit' in 'ConfigureServices' method.");
            }

            if (error == null)
            {
                error = GetDefaultErrorResponse();
            }

            if (interceptor == null)
            {
                interceptor = new HttpInvokeInterceptor();
            }

            services.AddSingleton<IAlgorithm>(algorithm);
            services.AddSingleton<HttpErrorResponse>(error);
            services.AddSingleton<HttpInvokeInterceptor>(interceptor);
            return services;
        }

        /// <summary>
        /// Add rate limit service
        /// </summary>
        /// <param name="services"></param>
        /// <param name="algorithmProvider"></param>
        /// <param name="errorProvider"></param>
        /// <param name="interceptorProvider"></param>
        /// <returns></returns>
        public static IServiceCollection AddRateLimit(this IServiceCollection services, Func<IServiceProvider, IAlgorithm> algorithmProvider, Func<IServiceProvider, HttpErrorResponse> errorProvider = null, Func<IServiceProvider, HttpInvokeInterceptor> interceptorProvider = null)
        {
            if (algorithmProvider == null)
            {
                throw new ArgumentNullException("The algorithm service provider is not registered, please use 'AddRateLimit' in 'ConfigureServices' method.");
            }

            if (errorProvider == null)
            {
                errorProvider = serviceProvider => GetDefaultErrorResponse();
            }

            if (interceptorProvider == null)
            {
                interceptorProvider = serviceProvider => new HttpInvokeInterceptor();
            }

            services.AddSingleton<IAlgorithm>(algorithmProvider);
            services.AddSingleton<HttpErrorResponse>(errorProvider);
            services.AddSingleton<HttpInvokeInterceptor>(interceptorProvider);
            return services;
        }


        private static HttpErrorResponse GetDefaultErrorResponse()
        {
            return new HttpErrorResponse()
            {
                HttpStatusCode = 429,
                BuildHttpContent = (context, checkResult) =>
                {
                    return "too many requests";
                }
            };
        }
    }
}