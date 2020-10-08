using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FireflySoft.RateLimit.Core.Sample
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("FireflySoft.RateLimit.Core.Sample");

            var rules = new RateLimitRule<SimulationRequest>[]
                            {
                                new RateLimitRule<SimulationRequest>()
                                {
                                    TimeWindow=1,
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
                                //.WithStorage(new RedisStorage(StackExchange.Redis.ConnectionMultiplexer.Connect("localhost")))
                                .WithRules(rules)
                                .Build();

            Task.Run(() =>
            {
                RunTest(processor);
            });

            Task.Run(() =>
            {
                RunTest(processor);
            });

            Console.ReadKey();
        }

        private static void RunTest(RateLimitProcessor<SimulationRequest> processor)
        {
            for (int i = 0; i < 50; i++)
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
                    Console.WriteLine($"error code: {result.Error.Code}");
                }
            }
        }

        private class SimulationRequest
        {
            public string RequestId { get; set; }

            public string RequestResource { get; set; }

            public Dictionary<string, string> Parameters { get; set; }
        }
    }
}
