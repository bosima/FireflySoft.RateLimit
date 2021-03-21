using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using FireflySoft.RateLimit.Core;

namespace FireflySoft.RateLmit.Core.BenchmarkTest
{
    [MemoryDiagnoser]
    [SimpleJob(launchCount: 1, warmupCount: 3, targetCount: 10)]
    public class BenchmarkTest
    {
        IRateLimitStorage redisStorage;
        IRateLimitStorage memoryStorage;

        [GlobalSetup]
        public void GlobalSetup()
        {
            memoryStorage = new InProcessMemoryStorage();
            redisStorage = new RedisStorage(StackExchange.Redis.ConnectionMultiplexer.Connect("127.0.0.1"));
        }

        [Benchmark]
        [Arguments("fixedWindow", "memory", 10000, 4)]
        [Arguments("slidingWindow", "memory", 10000, 4)]
        [Arguments("leakyBucket", "memory", 10000, 4)]
        [Arguments("tokenBucket", "memory", 10000, 4)]
        [Arguments("fixedWindow", "redis", 10000, 8)]
        [Arguments("slidingWindow", "redis", 10000, 8)]
        [Arguments("leakyBucket", "redis", 10000, 8)]
        [Arguments("tokenBucket", "redis", 10000, 8)]
        public void Test(string algorithm, string storageType, int limitNumber, int taskNumber)
        {
            RateLimitProcessor<SimulationRequest> processor;
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
                            Console.WriteLine($"error code: {result.Target}, {result.Error.Code}");
                        }
                    }
                });
            }

            Task.WaitAll(tasks);
        }

        [Benchmark]
        [Arguments("fixedWindow", "memory", 10000, 4)]
        [Arguments("slidingWindow", "memory", 10000, 4)]
        [Arguments("leakyBucket", "memory", 10000, 4)]
        [Arguments("tokenBucket", "memory", 10000, 4)]
        [Arguments("fixedWindow", "redis", 10000, 8)]
        [Arguments("slidingWindow", "redis", 10000, 8)]
        [Arguments("leakyBucket", "redis", 10000, 8)]
        [Arguments("tokenBucket", "redis", 10000, 8)]
        public async Task TestAsync(string algorithm, string storageType, int limitNumber, int taskNumber)
        {
            RateLimitProcessor<SimulationRequest> processor;
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
                            Console.WriteLine($"error code: {result.Target}, {result.Error.Code}");
                        }
                    }
                });
            }

            await Task.WhenAll(tasks);
        }

        private RateLimitProcessor<SimulationRequest> GetTokenBucketProcessor(string storageType, int limitNumber)
        {
            IRateLimitStorage storage = memoryStorage;
            if (storageType == "redis")
            {
                storage = redisStorage;
            }

            var tokenBucketRules = new TokenBucketRateLimitRule<SimulationRequest>[]
                {
                    new TokenBucketRateLimitRule<SimulationRequest>(limitNumber,100,TimeSpan.FromSeconds(1))
                    {
                        Id=Guid.NewGuid().ToString(),
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

            return new RateLimitProcessor<SimulationRequest>.Builder()
                .WithAlgorithm(new TokenBucketAlgorithm<SimulationRequest>(tokenBucketRules))
                .WithStorage(storage)
                .WithError(new RateLimitError()
                {
                    Code = 429,
                })
                .Build();
        }

        private RateLimitProcessor<SimulationRequest> GetLeakyBucketProcessor(string storageType, int limitNumber)
        {
            IRateLimitStorage storage = memoryStorage;
            if (storageType == "redis")
            {
                storage = redisStorage;
            }

            var leakyBucketRules = new LeakyBucketRateLimitRule<SimulationRequest>[]
                {
                    new LeakyBucketRateLimitRule<SimulationRequest>(limitNumber,100,TimeSpan.FromSeconds(1))
                    {
                        Id=Guid.NewGuid().ToString(),
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

            return new RateLimitProcessor<SimulationRequest>.Builder()
                .WithAlgorithm(new LeakyBucketAlgorithm<SimulationRequest>(leakyBucketRules))
                .WithStorage(storage)
                .WithError(new RateLimitError()
                {
                    Code = 429,
                })
                .Build();
        }

        private RateLimitProcessor<SimulationRequest> GetSlidingWindowProcessor(string storageType, int limitNumber)
        {
            IRateLimitStorage storage = memoryStorage;
            if (storageType == "redis")
            {
                storage = redisStorage;
            }

            var slidingWindowsRules = new SlidingWindowRateLimitRule<SimulationRequest>[]
                {
                    new SlidingWindowRateLimitRule<SimulationRequest>(TimeSpan.FromSeconds(10),TimeSpan.FromSeconds(1))
                    {
                        Id=Guid.NewGuid().ToString(),
                        LimitNumber=limitNumber,
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

            return new RateLimitProcessor<SimulationRequest>.Builder()
                .WithAlgorithm(new SlidingWindowAlgorithm<SimulationRequest>(slidingWindowsRules))
                .WithStorage(storage)
                .WithError(new RateLimitError()
                {
                    Code = 429,
                })
                .Build();
        }

        private RateLimitProcessor<SimulationRequest> GetFixedWindowProcessor(string storageType, int limitNumber)
        {
            IRateLimitStorage storage = memoryStorage;
            if (storageType == "redis")
            {
                storage = redisStorage;
            }

            var fixedWindowRules = new FixedWindowRateLimitRule<SimulationRequest>[]
                {
                    new FixedWindowRateLimitRule<SimulationRequest>()
                    {
                        Id=Guid.NewGuid().ToString(),
                        StatWindow=TimeSpan.FromSeconds(1),
                        LimitNumber=limitNumber,
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

            return new RateLimitProcessor<SimulationRequest>.Builder()
                .WithAlgorithm(new FixedWindowAlgorithm<SimulationRequest>(fixedWindowRules))
                .WithStorage(storage)
                .WithError(new RateLimitError()
                {
                    Code = 429,
                })
                .Build();
        }

        public class SimulationRequest
        {
            public string RequestId { get; set; }

            public string RequestResource { get; set; }

            public Dictionary<string, string> Parameters { get; set; }
        }
    }
}