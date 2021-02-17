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
                        Id="1",
                        StatWindow=TimeSpan.FromSeconds(1),
                        LimitNumber=30,
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

            var algorithm = new FixedWindowAlgorithm<SimulationRequest>(fixedWindowRules, true);

            var processor = new RateLimitProcessor<SimulationRequest>.Builder()
                .WithAlgorithm(algorithm)
                .WithStorage(storage)
                .WithError(new RateLimitError()
                {
                    Code = 429,
                })
                .Build();

            for (int i = 0; i < 80; i++)
            {
                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i == 40)
                {
                    algorithm.UpdateRules(new FixedWindowRateLimitRule<SimulationRequest>[]
                    {
                        new FixedWindowRateLimitRule<SimulationRequest>()
                        {
                            Id="1",
                            StatWindow=TimeSpan.FromSeconds(1),
                            LimitNumber=60,
                            ExtractTarget = (request) =>
                            {
                                return request.RequestResource;
                            },
                            CheckRuleMatching = (request) =>
                            {
                                return true;
                            },
                        }
                    });
                }

                if (i == 60)
                {
                    algorithm.UpdateRules(new FixedWindowRateLimitRule<SimulationRequest>[]
                    {
                        new FixedWindowRateLimitRule<SimulationRequest>()
                        {
                            Id="1",
                            StatWindow=TimeSpan.FromSeconds(1),
                            LimitNumber=40,
                            ExtractTarget = (request) =>
                            {
                                return request.RequestResource;
                            },
                            CheckRuleMatching = (request) =>
                            {
                                return true;
                            },
                        }
                    });
                }

                Console.WriteLine($"[{i}]check result:{result.IsLimit}.");
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
