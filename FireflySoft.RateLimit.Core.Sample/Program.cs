using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace FireflySoft.RateLimit.Core.Sample
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("FireflySoft.RateLimit.Core.Sample");

            IRateLimitStorage storage = new InProcessMemoryStorage();
            //IRateLimitStorage storage = new RedisStorage(StackExchange.Redis.ConnectionMultiplexer.Connect("127.0.0.1"));

            var fixedWindowRules = new FixedWindowRateLimitRule<SimulationRequest>[]
                {
                    new FixedWindowRateLimitRule<SimulationRequest>()
                    {
                        Id=Guid.NewGuid().ToString(),
                        StatWindow=TimeSpan.FromSeconds(1),
                        LimitNumber=50,
                        ExtractTarget = (request) =>
                        {
                            return request.RequestResource;
                        },
                        CheckRuleMatching = (request) =>
                        {
                            return true;
                        },
                    }
                };

            var processor = new RateLimitProcessor<SimulationRequest>.Builder()
                .WithAlgorithm(new FixedWindowAlgorithm<SimulationRequest>(fixedWindowRules))
                .WithStorage(storage)
                .WithError(new RateLimitError()
                {
                    Code = 429,
                })
                .Build();

            for (int i = 0; i < 60; i++)
            {
                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                Console.WriteLine($"[{i}]check result:{result.IsLimit}.");
                if (result.IsLimit)
                {
                    Console.WriteLine($"error code: {result.Target}, {result.Error.Code}");
                }
            }

            Console.Read();
        }

        private class SimulationRequest
        {
            public string RequestId { get; set; }

            public string RequestResource { get; set; }

            public Dictionary<string, string> Parameters { get; set; }
        }
    }
}
