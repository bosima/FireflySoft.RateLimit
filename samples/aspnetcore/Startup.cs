using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FireflySoft.RateLimit.Core;
using FireflySoft.RateLimit.Core.InProcessAlgorithm;
using FireflySoft.RateLimit.Core.Rule;
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
            AddLimitForPerSecond(services);
            //AddLimitForTokenBucketPerSecond(services);
            //AddLimitForLeakyBucketPerSecond(services);

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseRateLimit();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private void AddLimitForPerSecond(IServiceCollection app)
        {
            app.AddRateLimit(new InProcessFixedWindowAlgorithm(
                new[] {
                    new FixedWindowRule()
                    {
                        ExtractTarget = context =>
                        {
                            var goodsId = (context as HttpContext).Request.Query["GoodsId"].ToString();
                            return (context as HttpContext).Request.Path.Value;
                        },
                        CheckRuleMatching = context =>
                        {
                            return true;
                        },
                        Name="default limit rule",
                        LimitNumber=10,
                        StatWindow=TimeSpan.FromSeconds(3)
                    }
                })
            );
        }

        private void AddLimitForTokenBucketPerSecond(IServiceCollection app)
        {
            app.AddRateLimit(new InProcessTokenBucketAlgorithm(
                new[] {
                    new TokenBucketRule(20,10,TimeSpan.FromSeconds(1))
                    {
                        ExtractTarget = context =>
                        {
                            return (context as HttpContext).Request.Path.Value;
                        },
                        CheckRuleMatching = context =>
                        {
                            return true;
                        },
                        Name="default limit rule",
                    }
                })
            );
        }

        private void AddLimitForLeakyBucketPerSecond(IServiceCollection app)
        {
            app.AddRateLimit(new InProcessLeakyBucketAlgorithm(
                new[] {
                    new LeakyBucketRule(20,10,TimeSpan.FromSeconds(1))
                    {
                        ExtractTarget = context =>
                        {
                            return (context as HttpContext).Request.Path.Value;
                        },
                        CheckRuleMatching = context =>
                        {
                            return true;
                        },
                        Name="default limit rule",
                    }
                })
            );
        }

        private void AddLimitForDifferentUser(IServiceCollection app)
        {
            app.AddRateLimit(new InProcessFixedWindowAlgorithm(
                new[] {
                    new FixedWindowRule()
                    {
                        Id = "1",
                        ExtractTarget = context =>
                        {
                            // Assuming that the user ID is passed from the header, it needs to be obtained according to the actual situation
                            return (context as HttpContext).Request.GetTypedHeaders().Get<string>("userId");
                        },
                        CheckRuleMatching = context =>
                        {
                            // Assuming that the user type is passed from the header, it may actually need to be queried according to the user Id
                            // 0 free users 1 charged users
                            int userType = (context as HttpContext).Request.GetTypedHeaders().Get<int>("userType");
                            if(userType==0){
                                return true;
                            }
                            return false;
                        },
                        Name="Free user rate limit rules",
                        LimitNumber=100,
                        StatWindow=TimeSpan.FromDays(1),
                        StartTimeType=StartTimeType.FromNaturalPeriodBeign
                    },
                    new FixedWindowRule()
                    {
                        Id = "2",
                        ExtractTarget = context =>
                        {
                            return (context as HttpContext).Request.GetTypedHeaders().Get<string>("userId");
                        },
                        CheckRuleMatching = context =>
                        {
                            int userType = (context as HttpContext).Request.GetTypedHeaders().Get<int>("userType");
                            if(userType==1){
                                return true;
                            }
                            return false;
                        },
                        Name="Charged user rate limit rules",
                        LimitNumber=1000000,
                        StatWindow=TimeSpan.FromDays(1),
                        StartTimeType=StartTimeType.FromNaturalPeriodBeign
                    }
                }),
                new HttpErrorResponse()
                {
                    BuildHttpContent = (context, ruleCheckResult) =>
                    {
                        return "The number of queries exceeds the maximum limit of the day.";
                    }
                },
                new HttpInvokeInterceptor()
                {
                    OnTriggered = (context, ruleCheckResult) =>
                    {
                        Console.WriteLine("Rate Limit Alarm!!!");
                    }
                }
            );
        }
    }
}
