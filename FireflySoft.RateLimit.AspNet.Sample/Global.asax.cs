using System;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using FireflySoft.RateLimit.Core;

namespace FireflySoft.RateLimit.AspNet.Sample
{
    public class Global : HttpApplication
    {
        protected void Application_Start()
        {
            // 限流
            GlobalConfiguration.Configuration.MessageHandlers.Add(new RateLimitHandler(
                new Core.RateLimitProcessor<HttpRequestMessage>.Builder()
                .WithError(new Core.RateLimitError()
                {
                    Message = "The system is busy, please try again later"
                })
                .WithAlgorithm(new FixedWindowAlgorithm<HttpRequestMessage>( new[] {
                    new FixedWindowRateLimitRule<HttpRequestMessage>()
                    {
                        ExtractTarget = context =>
                        {
                             return context.RequestUri.AbsolutePath;
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
                .Build()
             ));

            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
}
}
