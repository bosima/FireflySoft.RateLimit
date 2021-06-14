using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using FireflySoft.RateLimit.Core;
using StackExchange.Redis;
using FireflySoft.RateLimit.Core.Rule;
using FireflySoft.RateLimit.Core.InProcessAlgorithm;
using FireflySoft.RateLimit.Core.RedisAlgorithm;
using System.Linq;

namespace FireflySoft.RateLmit.Core.BenchmarkTest
{
    [MemoryDiagnoser]
    [SimpleJob(launchCount: 1, warmupCount: 3, targetCount: 10)]
    public class BenchmarkTest
    {
        ConnectionMultiplexer redisClient;

        [GlobalSetup]
        public void GlobalSetup()
        {
            redisClient = StackExchange.Redis.ConnectionMultiplexer.Connect("127.0.0.1");
        }

        [Benchmark]
        // [Arguments("fixedWindow", "memory", 10000, 4)]
        // [Arguments("slidingWindow", "memory", 10000, 4)]
        // [Arguments("leakyBucket", "memory", 10000, 4)]
        // [Arguments("tokenBucket", "memory", 10000, 4)]
        // [Arguments("fixedWindow", "redis", 10000, 8)]
        // [Arguments("slidingWindow", "redis", 10000, 8)]
        // [Arguments("leakyBucket", "redis", 10000, 8)]
        [Arguments("tokenBucket", "redis", 10000, 8)]
        public void Test(string algorithm, string storageType, int limitNumber, int taskNumber)
        {
            IAlgorithm processor;
            switch (algorithm)
            {
                default:
                    processor = GetFixedWindowProcessor(storageType, limitNumber);
                    break;
                case "slidingWindow":
                    processor = GetSlidingWindowProcessor(storageType, limitNumber);
                    break;
                case "leakyBucket":
                    processor = GetLeakyBucketProcessor(storageType, limitNumber);
                    break;
                case "tokenBucket":
                    processor = GetTokenBucketProcessor(storageType, limitNumber);
                    break;
            }

            var loopNumber = limitNumber / taskNumber;
            Task[] tasks = new Task[taskNumber];
            for (int k = 0; k < taskNumber; k++)
            {
                tasks[k] = Task.Run(() =>
                {
                    for (int i = 0; i < loopNumber; i++)
                    {
                        var result = processor.Check(new SimulationRequest()
                        {
                            RequestId = Guid.NewGuid().ToString(),
                            RequestResource = "home",
                            Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                        });

                        if (result.IsLimit)
                        {
                            Console.WriteLine($"limit count: {result.RuleCheckResults.First().Count}");
                        }
                    }
                });
            }

            Task.WaitAll(tasks);
        }

        [Benchmark]
        // [Arguments("fixedWindow", "memory", 10000, 4)]
        //[Arguments("slidingWindow", "memory", 10000, 4)]
        // [Arguments("leakyBucket", "memory", 10000, 4)]
        // [Arguments("tokenBucket", "memory", 10000, 4)]
        // [Arguments("fixedWindow", "redis", 10000, 8)]
        // [Arguments("slidingWindow", "redis", 10000, 8)]
        // [Arguments("leakyBucket", "redis", 10000, 8)]
         [Arguments("tokenBucket", "redis", 10000, 8)]
        public async Task TestAsync(string algorithm, string storageType, int limitNumber, int taskNumber)
        {
            IAlgorithm processor;
            switch (algorithm)
            {
                default:
                    processor = GetFixedWindowProcessor(storageType, limitNumber);
                    break;
                case "slidingWindow":
                    processor = GetSlidingWindowProcessor(storageType, limitNumber);
                    break;
                case "leakyBucket":
                    processor = GetLeakyBucketProcessor(storageType, limitNumber);
                    break;
                case "tokenBucket":
                    processor = GetTokenBucketProcessor(storageType, limitNumber);
                    break;
            }

            var loopNumber = limitNumber / taskNumber;
            Task[] tasks = new Task[taskNumber];
            for (int k = 0; k < taskNumber; k++)
            {
                tasks[k] = Task.Run(async () =>
                {
                    for (int i = 0; i < loopNumber; i++)
                    {
                        var result = await processor.CheckAsync(new SimulationRequest()
                        {
                            RequestId = Guid.NewGuid().ToString(),
                            RequestResource = "home",
                            Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                        });

                        if (result.IsLimit)
                        {
                            Console.WriteLine($"limit count: {result.RuleCheckResults.First().Count}");
                        }
                    }
                });
            }

            await Task.WhenAll(tasks);
        }

        private IAlgorithm GetTokenBucketProcessor(string storageType, int limitNumber)
        {
            var tokenBucketRules = new TokenBucketRule[]
                {
                    new TokenBucketRule(limitNumber,100,TimeSpan.FromSeconds(1))
                    {
                        Id=Guid.NewGuid().ToString(),
                        LockSeconds=1,
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

            if (storageType == "redis")
            {
                return new RedisTokenBucketAlgorithm(tokenBucketRules, redisClient);
            }
            else
            {
                return new InProcessTokenBucketAlgorithm(tokenBucketRules);
            }
        }

        private IAlgorithm GetLeakyBucketProcessor(string storageType, int limitNumber)
        {
            var leakyBucketRules = new LeakyBucketRule[]
                {
                    new LeakyBucketRule(limitNumber,100,TimeSpan.FromSeconds(1))
                    {
                        Id=Guid.NewGuid().ToString(),
                        LockSeconds=1,
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

            if (storageType == "redis")
            {
                return new RedisLeakyBucketAlgorithm(leakyBucketRules, redisClient);
            }
            else
            {
                return new InProcessLeakyBucketAlgorithm(leakyBucketRules);
            }
        }

        private IAlgorithm GetSlidingWindowProcessor(string storageType, int limitNumber)
        {
            var slidingWindowsRules = new SlidingWindowRule[]
                {
                    new SlidingWindowRule(TimeSpan.FromSeconds(10),TimeSpan.FromSeconds(1))
                    {
                        Id=Guid.NewGuid().ToString(),
                        LimitNumber=limitNumber,
                        LockSeconds=1,
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

            if (storageType == "redis")
            {
                return new RedisSlidingWindowAlgorithm(slidingWindowsRules, redisClient);
            }
            else
            {
                return new InProcessSlidingWindowAlgorithm(slidingWindowsRules);
            }
        }

        private IAlgorithm GetFixedWindowProcessor(string storageType, int limitNumber)
        {
            var fixedWindowRules = new FixedWindowRule[]
                {
                    new FixedWindowRule()
                    {
                        Id=Guid.NewGuid().ToString(),
                        StatWindow=TimeSpan.FromSeconds(1),
                        LimitNumber=limitNumber,
                        ExtractTarget = (request) =>
                        {
                            return (request as SimulationRequest).RequestResource;
                        },
                        CheckRuleMatching = (request) =>
                        {
                            return true;
                        }
                    }
                };

            if (storageType == "redis")
            {
                return new RedisFixedWindowAlgorithm(fixedWindowRules, redisClient);
            }
            else
            {
                return new InProcessFixedWindowAlgorithm(fixedWindowRules);
            }
        }

        public class SimulationRequest
        {
            public string RequestId { get; set; }

            public string RequestResource { get; set; }

            public Dictionary<string, string> Parameters { get; set; }
        }
    }
}