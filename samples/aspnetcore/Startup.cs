using System;
using FireflySoft.RateLimit.Core;
using FireflySoft.RateLimit.Core.InProcessAlgorithm;
using FireflySoft.RateLimit.Core.RedisAlgorithm;
using FireflySoft.RateLimit.Core.Rule;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
            AddLimitForFixedWindowPerSecond(services);
            //AddLimitForRedisFixedWindowPerSecond(services);
            //AddLimitForSlidingWindowPerSecond(services);
            //AddLimitForRedisSlidingWindowPerSecond(services);
            //AddLimitForTokenBucketPerSecond(services);
            //AddLimitForRedisTokenBucketPerSecond(services);
            //AddLimitForLeakyBucketPerSecond(services);
            //AddLimitForRedisLeakyBucketPerSecond(services);

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRateLimit();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private void AddLimitForFixedWindowPerSecond(IServiceCollection app)
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

        private void AddLimitForRedisFixedWindowPerSecond(IServiceCollection app)
        {
            var redisClient = StackExchange.Redis.ConnectionMultiplexer.Connect("127.0.0.1");
            app.AddRateLimit(new RedisFixedWindowAlgorithm(
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
                }, redisClient)
            );
        }

        private void AddLimitForSlidingWindowPerSecond(IServiceCollection app)
        {
            app.AddRateLimit(new InProcessSlidingWindowAlgorithm(
                new[] {
                    new SlidingWindowRule(TimeSpan.FromMilliseconds(3000),TimeSpan.FromMilliseconds(1000))
                    {
                        LimitNumber=40,
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

        private void AddLimitForRedisSlidingWindowPerSecond(IServiceCollection app)
        {
            var redisClient = StackExchange.Redis.ConnectionMultiplexer.Connect("127.0.0.1");
            app.AddRateLimit(new RedisSlidingWindowAlgorithm(
                new[] {
                    new SlidingWindowRule(TimeSpan.FromMilliseconds(3000),TimeSpan.FromMilliseconds(1000))
                    {
                        LimitNumber=40,
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
                }, redisClient)
            );
        }

        private void AddLimitForTokenBucketPerSecond(IServiceCollection app)
        {
            app.AddRateLimit(new InProcessTokenBucketAlgorithm(
                new[] {
                    new TokenBucketRule(10,10,TimeSpan.FromSeconds(3))
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

        private void AddLimitForRedisTokenBucketPerSecond(IServiceCollection app)
        {
            var redisClient = StackExchange.Redis.ConnectionMultiplexer.Connect("127.0.0.1");
            app.AddRateLimit(new RedisTokenBucketAlgorithm(
                new[] {
                    new TokenBucketRule(10,10,TimeSpan.FromSeconds(3))
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
                }, redisClient)
            );
        }

        private void AddLimitForLeakyBucketPerSecond(IServiceCollection app)
        {
            app.AddRateLimit(new InProcessLeakyBucketAlgorithm(
                new[] {
                    new LeakyBucketRule(10,10,TimeSpan.FromSeconds(3))
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

        private void AddLimitForRedisLeakyBucketPerSecond(IServiceCollection app)
        {
            var redisClient = StackExchange.Redis.ConnectionMultiplexer.Connect("127.0.0.1");
            app.AddRateLimit(new RedisLeakyBucketAlgorithm(
                new[] {
                    new LeakyBucketRule(10,10,TimeSpan.FromSeconds(3))
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
                },redisClient)
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
