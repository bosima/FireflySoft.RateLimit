using System;

namespace aspnetcore6.RateLimit
{
    public static class AutoUpdateAlgorithmService
    {
        /// <summary>
        /// Add auto update rate limit algorithm service
        /// </summary>
        /// <param name="services"></param>
        /// <param name="algorithm"></param>
        /// <param name="error"></param>
        /// <param name="interceptor"></param>
        /// <returns></returns>
        public static IServiceCollection AddAutoUpdateRateLimitAlgorithm(this IServiceCollection services)
        {
            services.AddSingleton<RateLimitRuleDAO>();
            services.AddSingleton<RateLimitConfiguratioinManager>();
            services.AddSingleton<AutoUpdateAlgorithmManager>();
            return services;
        }
    }
}
