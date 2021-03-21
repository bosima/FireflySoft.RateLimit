using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FireflySoft.RateLimit.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FireflySoft.RateLimit.AspNetCore.Sample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //app.UseHttpsRedirection();

            UseLimitForPerSecond(app);

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private void UseLimitForPerSecond(IApplicationBuilder app)
        {
            app.UseRateLimit(new Core.RateLimitProcessor<HttpContext>.Builder()
                            .WithError(new Core.RateLimitError()
                            {
                                Message = "The system is busy, please try again later"
                            })
                            .WithAlgorithm(new FixedWindowAlgorithm<HttpContext>(new[] {
                                new FixedWindowRateLimitRule<HttpContext>()
                                {
                                    ExtractTarget = context =>
                                    {
                                        return context.Request.Path.Value;
                                    },
                                    CheckRuleMatching = context =>
                                    {
                                        return true;
                                    },
                                    Name="general limit rule",
                                    LimitNumber=30,
                                    StatWindow=TimeSpan.FromSeconds(1)
                                }
                            }))
                            .Build());
        }

        private void UseLimitForDifferentUser(IApplicationBuilder app)
        {
            app.UseRateLimit(new RateLimitProcessor<HttpContext>.Builder()
                    .WithAlgorithm(new FixedWindowAlgorithm<HttpContext>(new[] {
                        new FixedWindowRateLimitRule<HttpContext>()
                        {
                            Id = "1",
                            ExtractTarget = context =>
                            {
                                // 这里假设用户Id是从header中传过来的，需根据实际情况获取
                                return context.Request.GetTypedHeaders().Get<string>("userId");
                            },
                            CheckRuleMatching = context =>
                            {
                                // 这里假设用户类型是从header中传过来的，实际可能需要根据用户Id再去查询
                                // 0免费用户 1收费用户
                                int userType = context.Request.GetTypedHeaders().Get<int>("userType");
                                if(userType==0){
                                    return true;
                                }
                                return false;
                            },
                            Name="免费用户限流规则",
                            LimitNumber=100,
                            StatWindow=TimeSpan.FromDays(1),
                            StartTimeType=StartTimeType.FromNaturalPeriodBeign
                        },
                        new FixedWindowRateLimitRule<HttpContext>()
                        {
                            Id = "2",
                            ExtractTarget = context =>
                            {
                                // 这里假设用户Id是从header中传过来的，需根据实际情况获取
                                return context.Request.GetTypedHeaders().Get<string>("userId");
                            },
                            CheckRuleMatching = context =>
                            {
                                // 这里假设用户类型是从header中传过来的，实际可能需要根据用户Id再去查询
                                // 0免费用户 1收费用户
                                int userType = context.Request.GetTypedHeaders().Get<int>("userType");
                                if(userType==1){
                                    return true;
                                }
                                return false;
                            },
                            Name="收费用户限流规则",
                            LimitNumber=1000000,
                            StatWindow=TimeSpan.FromDays(1),
                            StartTimeType=StartTimeType.FromNaturalPeriodBeign
                        }
                    }))
                    .WithError(new Core.RateLimitError()
                    {
                        Code = 429,
                        Message = "查询数达到当天最大限制"
                    })
                    //.WithStorage(new RedisStorage(StackExchange.Redis.ConnectionMultiplexer.Connect("localhost")))
                    .Build());
        }
    }
}
