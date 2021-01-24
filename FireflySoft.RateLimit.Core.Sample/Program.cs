using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FireflySoft.RateLimit.Core.Sample
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("FireflySoft.RateLimit.Core.Sample");

            var fixedWindowRules = new FixedWindowRateLimitRule<SimulationRequest>[]
                            {
                                new FixedWindowRateLimitRule<SimulationRequest>()
                                {
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

            var slidingWindowsRules = new SlidingWindowRateLimitRule<SimulationRequest>[]
                            {
                                new SlidingWindowRateLimitRule<SimulationRequest>(TimeSpan.FromSeconds(10),TimeSpan.FromSeconds(1))
                                {
                                    LimitNumber=50,
                                    LockSeconds=1,
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

            var leakyBucketRules = new LeakyBucketRateLimitRule<SimulationRequest>[]
                            {
                                new LeakyBucketRateLimitRule<SimulationRequest>(30,10,TimeSpan.FromSeconds(1))
                                {
                                    LockSeconds=1,
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

            var tokenBucketRules = new TokenBucketRateLimitRule<SimulationRequest>[]
                            {
                                new TokenBucketRateLimitRule<SimulationRequest>(30,10,TimeSpan.FromSeconds(1))
                                {
                                    LockSeconds=1,
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
                                //.WithAlgorithm(new FixedWindowAlgorithm<SimulationRequest>(fixedWindowRules))
                                .WithAlgorithm(new SlidingWindowAlgorithm<SimulationRequest>(slidingWindowsRules))
                                //.WithAlgorithm(new LeakyBucketAlgorithm<SimulationRequest>(leakyBucketRules))
                                //.WithAlgorithm(new TokenBucketAlgorithm<SimulationRequest>(tokenBucketRules))
                                .WithError(new RateLimitError()
                                {
                                    Code = 429,

                                })
                                .Build();

            //FixedWindowTest(processor);
            SlidingWindowTest(processor);
            //LeakyBucketTest(processor);
            //TokenBucketTest(processor);

            Console.Read();
        }

        private static void FixedWindowTest(RateLimitProcessor<SimulationRequest> processor)
        {
            for (int i = 0; i < 100; i++)
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
        }

        private static void SlidingWindowTest(RateLimitProcessor<SimulationRequest> processor)
        {
            // for test: start with the begin of a second
            var lastSecond = DateTime.Now.Second;
            while (true)
            {
                if (lastSecond != DateTime.Now.Second)
                {
                    break;
                }
                Thread.Sleep(10);
            }

            for (int i = 0; i < 40; i++)
            {
                int j = 3;
                if (i >= 6 && i <= 15)
                {
                    j = 5;
                }

                if (i >= 20 && i <= 29)
                {
                    j = 6;
                }

                for (int k = 0; k < j; k++)
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

                Thread.Sleep(1000);
            }
        }

        private static void LeakyBucketTest(RateLimitProcessor<SimulationRequest> processor)
        {
            for (int i = 0; i < 80; i++)
            {
                if (i == 19 || i == 29)
                {
                    Thread.Sleep(1000);
                }

                if (i == 69)
                {
                    Thread.Sleep(3000);
                }

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

        private static void TokenBucketTest(RateLimitProcessor<SimulationRequest> processor)
        {
            for (int i = 0; i < 80; i++)
            {
                if (i == 19 || i == 70)
                {
                    Thread.Sleep(1000);
                }

                if (i == 29)
                {
                    Thread.Sleep(5000);
                }

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
