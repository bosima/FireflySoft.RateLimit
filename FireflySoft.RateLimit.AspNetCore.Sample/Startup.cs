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

            app.UseRateLimit(new Core.RateLimitProcessor<HttpContext>.Builder()
                .WithError(new Core.RateLimitError()
                {
                    Message = "The system is busy, please try again later"
                })
                .WithAlgorithm(new FixedWindowAlgorithm<HttpContext>( new[] {
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
                        Id=Guid.NewGuid().ToString(),
                        Name="general limit rule",
                        LimitNumber=30,
                        StatWindow=TimeSpan.FromSeconds(1)
                    }
                }))
                .Build());

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
