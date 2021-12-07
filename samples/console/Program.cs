using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using FireflySoft.RateLimit.Core.InProcessAlgorithm;
using FireflySoft.RateLimit.Core.Rule;
using FireflySoft.RateLimit.Core.RedisAlgorithm;
using System.Linq;
using FireflySoft.RateLimit.Core.Time;

namespace FireflySoft.RateLimit.Core.Sample
{
    class MainClass
    {
        public static async Task Main(string[] args)
        {
            await Task.FromResult("Start");

            Console.WriteLine("FireflySoft.RateLimit.Core.Sample");

            //DoFixedWindow();
            //await DoFixedWindowAsync();
            //DoLeakyBucket();
            DoLTokenBucket();
            //TestLuaScriptMissing();

            Console.Read();
        }

        private static void DoFixedWindow()
        {
            var fixedWindowRules = new FixedWindowRule[]
                {
                    new FixedWindowRule()
                    {
                        Id="1",
                        StatWindow=TimeSpan.FromSeconds(1),
                        LimitNumber=30,
                        ExtractTarget = (request) =>
                        {
                            return (request as SimulationRequest).RequestResource;
                        },
                        CheckRuleMatching = (request) =>
                        {
                            return true;
                        },
                    }
                };
            var timeProvider = new LocalTimeProvider();
            var algorithm = new InProcessFixedWindowAlgorithm(fixedWindowRules, timeProvider, true);
            // var redisClient = StackExchange.Redis.ConnectionMultiplexer.Connect("127.0.0.1");
            // var algorithm = new RedisFixedWindowAlgorithm(fixedWindowRules, redisClient, timeProvider, true);

            for (int i = 0; i < 80; i++)
            {
                var result = algorithm.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                var isLimit = result.IsLimit;
                Console.WriteLine($"IsLimit:{isLimit}");

                foreach (var r in result.RuleCheckResults)
                {
                    Console.WriteLine($"[{i}] Target:{r.Target},IsLimit:{r.IsLimit},Count:{r.Count}.");

                    // If you need to return when a rule is restricted, you can use break.
                    // However, this is not recommended, the count will be lost for the rule which is not triggered
                    // if (r.IsLimit)
                    // {
                    //     break;
                    // }
                }

                // // Do not use the LINQ method after traversal or multiple times in a single request.
                // // This results in duplicate counts.
                // var limit = result.Any(d=>d.IsLimit);
                // var r = result.First();
                // Console.WriteLine($"[{i}] Target:{r.Target},IsLimit:{r.IsLimit},Count:{r.Count}.");

                if (i == 40)
                {
                    algorithm.UpdateRules(new FixedWindowRule[]
                    {
                        new FixedWindowRule()
                        {
                            Id="1",
                            StatWindow=TimeSpan.FromSeconds(1),
                            LimitNumber=60,
                            ExtractTarget = (request) =>
                            {
                                return (request as SimulationRequest).RequestResource;
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
                    algorithm.UpdateRules(new FixedWindowRule[]
                    {
                        new FixedWindowRule()
                        {
                            Id="1",
                            StatWindow=TimeSpan.FromSeconds(1),
                            LimitNumber=40,
                            ExtractTarget = (request) =>
                            {
                                return (request as SimulationRequest).RequestResource;
                            },
                            CheckRuleMatching = (request) =>
                            {
                                return true;
                            },
                        }
                    });
                }
            }

            Console.WriteLine("Over");
        }

        private static async Task DoFixedWindowAsync()
        {
            var fixedWindowRules = new FixedWindowRule[]
                {
                    new FixedWindowRule()
                    {
                        Id="1",
                        StatWindow=TimeSpan.FromSeconds(1),
                        LimitNumber=30,
                        ExtractTargetAsync = (request) =>
                        {
                            return Task.FromResult((request as SimulationRequest).RequestResource);
                        },
                        CheckRuleMatchingAsync = (request) =>
                        {
                            return Task.FromResult(true);
                        },
                    }
                };
            var timeProvider = new LocalTimeProvider();
            var algorithm = new InProcessFixedWindowAlgorithm(fixedWindowRules, timeProvider, true);
            // var redisClient = StackExchange.Redis.ConnectionMultiplexer.Connect("127.0.0.1");
            // var algorithm = new RedisFixedWindowAlgorithm(fixedWindowRules, redisClient, timeProvider, true);

            for (int i = 0; i < 80; i++)
            {
                var result = algorithm.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                var resultValue = await result;
                foreach (var r in resultValue.RuleCheckResults)
                {
                    Console.WriteLine($"[{i}] Target:{r.Target},IsLimit:{r.IsLimit},Count:{r.Count}.");
                }

                if (i == 40)
                {
                    algorithm.UpdateRules(new FixedWindowRule[]
                    {
                        new FixedWindowRule()
                        {
                            Id="1",
                            StatWindow=TimeSpan.FromSeconds(1),
                            LimitNumber=60,
                            ExtractTargetAsync = (request) =>
                            {
                                return Task.FromResult((request as SimulationRequest).RequestResource);
                            },
                            CheckRuleMatchingAsync = (request) =>
                            {
                                return Task.FromResult(true);
                            },
                        }
                    });
                }

                if (i == 60)
                {
                    algorithm.UpdateRules(new FixedWindowRule[]
                    {
                        new FixedWindowRule()
                        {
                            Id="1",
                            StatWindow=TimeSpan.FromSeconds(1),
                            LimitNumber=40,
                            ExtractTargetAsync = (request) =>
                            {
                                return Task.FromResult((request as SimulationRequest).RequestResource);
                            },
                            CheckRuleMatchingAsync = (request) =>
                            {
                                return Task.FromResult(true);
                            },
                        }
                    });
                }
            }
        }

        private static void DoLeakyBucket()
        {
            var leakyBucketRules = new LeakyBucketRule[]
            {
                new LeakyBucketRule(30,10,TimeSpan.FromSeconds(1))
                {
                    Id="2",
                    ExtractTarget = (request) =>
                    {
                        return (request as SimulationRequest).RequestResource;
                    },
                    CheckRuleMatching = (request) =>
                    {
                        return true;
                    },
                }
            };

            var timeProvider = new LocalTimeProvider();
            var algorithm = new InProcessLeakyBucketAlgorithm(leakyBucketRules, timeProvider, true);
            // var redisClient = StackExchange.Redis.ConnectionMultiplexer.Connect("127.0.0.1");
            // var algorithm = new RedisLeakyBucketAlgorithm(leakyBucketRules, redisClient, timeProvider, true);

            for (int i = 0; i < 160; i++)
            {
                if (i == 50)
                {
                    algorithm.UpdateRules(new LeakyBucketRule[]
                    {
                        new LeakyBucketRule(50,10,TimeSpan.FromSeconds(1))
                        {
                            Id="2",
                            ExtractTarget = (request) =>
                            {
                                return (request as SimulationRequest).RequestResource;
                            },
                            CheckRuleMatching = (request) =>
                            {
                                return true;
                            },
                        }
                    });
                }

                if (i == 70)
                {
                    // Attention:
                    // If you use delayed processing, such as Task.Delay,
                    // increasing the outflow rate will have a greater damage to the data processing sequence.
                    algorithm.UpdateRules(new LeakyBucketRule[]
                    {
                        new LeakyBucketRule(50,20,TimeSpan.FromSeconds(1))
                        {
                            Id="2",
                            ExtractTarget = (request) =>
                            {
                                return (request as SimulationRequest).RequestResource;
                            },
                            CheckRuleMatching = (request) =>
                            {
                                return true;
                            },
                        }
                    });
                    Thread.Sleep(1000);
                }

                if (i == 110 || i == 120 || i == 130 || i == 140 || i == 150)
                {
                    Thread.Sleep(1000);
                }

                var result = algorithm.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                // Wait in the return value is very useful, you can use it in the delay queue, 
                // you can also make the current thread pause for a specified period of time.
                foreach (var r in result.RuleCheckResults)
                {
                    Console.WriteLine($"[{i}] Target:{r.Target},IsLimit:{r.IsLimit},Count:{r.Count},Wait:{r.Wait}.");
                }
            }
        }

        private static void DoLTokenBucket()
        {
            var tokenBucketRules = new TokenBucketRule[]
            {
                new TokenBucketRule(30,10,TimeSpan.FromSeconds(3))
                {
                    Id="test",
                    ExtractTarget = (request) =>
                    {
                        return (request as SimulationRequest).RequestResource;
                    },
                    CheckRuleMatching = (request) =>
                    {
                        return true;
                    },
                    LockSeconds = 3
                }
            };

            var timeProvider = new LocalTimeProvider();
            // var algorithm = new InProcessLeakyBucketAlgorithm(tokenBucketRules, timeProvider, true);
            var redisClient = StackExchange.Redis.ConnectionMultiplexer.Connect("127.0.0.1:6391");
            var algorithm = new RedisTokenBucketAlgorithm(tokenBucketRules, redisClient, timeProvider, true);

            for (int i = 0; i < 16000; i++)
            {
                if (i % 30 == 0)
                {
                    Thread.Sleep(3000);
                }

                var result = algorithm.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                // Wait in the return value is very useful, you can use it in the delay queue, 
                // you can also make the current thread pause for a specified period of time.
                foreach (var r in result.RuleCheckResults)
                {
                    Console.WriteLine($"[{i}] Target:{r.Target},IsLimit:{r.IsLimit},Count:{r.Count},Wait:{r.Wait}.");
                }
            }
        }

        private static void TestLuaScriptMissing()
        {
            var fixedWindowRules = new FixedWindowRule[]
                {
                    new FixedWindowRule()
                    {
                        Id=Guid.NewGuid().ToString(),
                        StatWindow=TimeSpan.FromSeconds(1),
                        LimitNumber=10000,
                        ExtractTarget = (request) =>
                        {
                            return (request as SimulationRequest).RequestResource;
                        },
                        CheckRuleMatching = (request) =>
                        {
                            return true;
                        },
                    }
                };
            var timeProvider = new LocalTimeProvider();
            var redisClient = StackExchange.Redis.ConnectionMultiplexer.Connect("127.0.0.1");
            var algorithm = new RedisFixedWindowAlgorithm(fixedWindowRules, redisClient, timeProvider, true);

            Parallel.For(1, 1000000, (index, state) =>
            {
                try
                {
                    var result = algorithm.Check(new SimulationRequest()
                    {
                        RequestId = Guid.NewGuid().ToString(),
                        RequestResource = "home",
                        Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                    });

                    if (index % 1000 == 0)
                    {
                        Console.WriteLine($"index:{index}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            });
        }

        private class SimulationRequest
        {
            public string RequestId { get; set; }

            public string RequestResource { get; set; }

            public Dictionary<string, string> Parameters { get; set; }
        }
    }
}
