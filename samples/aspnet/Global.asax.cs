using System;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using FireflySoft.RateLimit.Core;
using FireflySoft.RateLimit.Core.Rule;

namespace FireflySoft.RateLimit.AspNet.Sample
{
    public class Global : HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configuration.MessageHandlers.Add(new RateLimitHandler(
                new Core.InProcessAlgorithm.InProcessFixedWindowAlgorithm(
                    new[] {
                        new FixedWindowRule()
                        {
                            ExtractTarget = context =>
                            {
                                return (context as HttpRequestMessage).RequestUri.AbsolutePath;
                            },
                            CheckRuleMatching = context =>
                            {
                                return true;
                            },
                            Name="default limit rule",
                            LimitNumber=30,
                            StatWindow=TimeSpan.FromSeconds(1)
                        }
                    })
            ));

            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}
