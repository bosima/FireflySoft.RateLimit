using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FireflySoft.RateLimit.Core.Test
{
    [TestClass]
    public class UnitTest
    {
        [ClassInitialize]
        public static void Init(TestContext tc){
        }

        [DataTestMethod]
        [DataRow("memory")]
        [DataRow("redis")]
        public void TestFixedWindowAlgorithm(string storageType)
        {
            var processor =GetFixedWindowProcessor(storageType);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 61)
                {
                    
                    Thread.Sleep(1000);
                }
                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i > 50 && i <= 60)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i <= 50 && i > 60)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        [DataRow("memory")]
        [DataRow("redis")]
        public async Task TestFixedWindowAlgorithmAsync(string storageType)
        {
            var processor =GetFixedWindowProcessor(storageType);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 61)
                {
                    Thread.Sleep(1000);
                }
                var result = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i > 50 && i <= 60)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i <= 50 && i > 60)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        [DataRow("memory")]
        [DataRow("redis")]
        public void TestSlidingWindowAlgorithm(string storageType)
        {
            var processor=GetSlidingWindowProcessor(storageType);

            while(DateTimeOffset.Now.Second%10>0){
                Thread.Sleep(10);
            }

            for (int i = 1; i <= 30; i++)
            {
                int j = 3;
                if (i == 7 || i == 14)
                {
                    j = 6;
                }
                          
                Debug.WriteLine("for "+i);

                RateLimitResponse<SimulationRequest> checkResult = null;
                for (int k = 0; k < j; k++)
                {
                    checkResult = processor.Check(new SimulationRequest()
                    {
                        RequestId = Guid.NewGuid().ToString(),
                        RequestResource = "home",
                        Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                    });
                }
               
                if (i == 7 || i == 14)
                {
                    Assert.AreEqual(true, checkResult.IsLimit);
                }
                else
                {
                    Assert.AreEqual(false, checkResult.IsLimit);
                }

                Thread.Sleep(1010);
            }
        }

        [DataTestMethod]
        [DataRow("memory")]
        [DataRow("redis")]
        public async Task TestSlidingWindowAlgorithmAsync(string storageType)
        {
            var processor=GetSlidingWindowProcessor(storageType);

            while(DateTimeOffset.Now.Second%10>0){
                Thread.Sleep(10);
            }

            for (int i = 1; i <= 30; i++)
            {
                int j = 3;
                if (i == 7 || i == 14)
                {
                    j = 6;
                }
                Debug.WriteLine("for "+i);

                RateLimitResponse<SimulationRequest> checkResult = null;
                for (int k = 0; k < j; k++)
                {
                    checkResult = await processor.CheckAsync(new SimulationRequest()
                    {
                        RequestId = Guid.NewGuid().ToString(),
                        RequestResource = "home",
                        Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                    });
                }
               
               if (i == 7 || i ==14)
                {
                    Assert.AreEqual(true, checkResult.IsLimit);
                }
                else
                {
                    Assert.AreEqual(false, checkResult.IsLimit);
                }

                Thread.Sleep(1000);
            }
        }

        [DataTestMethod]
        [DataRow("memory")]
        [DataRow("redis")]
        public void TestLeakyBucketAlgorithm(string storageType)
        {
            var processor = GetLeakyBucketProcessor(storageType);

            for (int i = 1; i <= 80; i++)
            {
                Debug.WriteLine("for "+i);

                var checkResult = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i == 31 || i == 42 || i >= 53)
                {
                    Assert.AreEqual(true, checkResult.IsLimit);
                }
                else
                {
                    Assert.AreEqual(false, checkResult.IsLimit);
                }

                if (i == 31 || i == 42)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        [DataTestMethod]
        [DataRow("memory")]
        [DataRow("redis")]
        public async Task TestLeakyBucketAlgorithmAsync(string storageType)
        {
            var processor = GetLeakyBucketProcessor(storageType);

            for (int i = 1; i <= 80; i++)
            {
                Debug.WriteLine("for "+i);

                var checkResult =await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i == 31 || i == 42 || i >= 53)
                {
                    Assert.AreEqual(true, checkResult.IsLimit);
                }
                else
                {
                    Assert.AreEqual(false, checkResult.IsLimit);
                }

                if (i == 31 || i == 42)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        [DataTestMethod]
        [DataRow("memory")]
        [DataRow("redis")]
        public void TestTokenBucketAlgorithm(string storageType)
        {
            var processor = GetTokenBucketProcessor(storageType);

            for (int i = 1; i <= 80; i++)
            {
                Debug.WriteLine("for "+i);

                var checkResult = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i == 31 || i == 42 || i >= 53)
                {
                    Assert.AreEqual(true, checkResult.IsLimit);
                }
                else
                {
                    Assert.AreEqual(false, checkResult.IsLimit);
                }

                if (i == 31 || i == 42)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        [DataTestMethod]
        [DataRow("memory")]
        [DataRow("redis")]
        public async Task TestTokenBucketAlgorithmAsync(string storageType)
        {
            var processor = GetTokenBucketProcessor(storageType);

            for (int i = 1; i <= 80; i++)
            {
                Debug.WriteLine("for "+i);

                var checkResult = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i == 31 || i == 42 || i >= 53)
                {
                    Assert.AreEqual(true, checkResult.IsLimit);
                }
                else
                {
                    Assert.AreEqual(false, checkResult.IsLimit);
                }

                if (i == 31 || i == 42)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        private RateLimitProcessor<SimulationRequest> GetFixedWindowProcessor(string storageType){
            var fixedWindowRules = new FixedWindowRateLimitRule<SimulationRequest>[]
                {
                    new FixedWindowRateLimitRule<SimulationRequest>()
                    {
                        Id=Guid.NewGuid().ToString(),
                        StatWindow=TimeSpan.FromSeconds(1),
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

            IRateLimitStorage storage = new InProcessMemoryStorage();
            if (storageType == "redis")
            {
                storage = new RedisStorage(StackExchange.Redis.ConnectionMultiplexer.Connect("127.0.0.1"));
            }

            return new RateLimitProcessor<SimulationRequest>.Builder()
                .WithAlgorithm(new FixedWindowAlgorithm<SimulationRequest>(fixedWindowRules))
                .WithStorage(storage)
                .WithError(new RateLimitError()
                {
                    Code = 429,
                })
                .Build();
        }

        private RateLimitProcessor<SimulationRequest> GetSlidingWindowProcessor(string storageType){
            var slidingWindowsRules = new SlidingWindowRateLimitRule<SimulationRequest>[]
                {
                    new SlidingWindowRateLimitRule<SimulationRequest>(TimeSpan.FromSeconds(6),TimeSpan.FromSeconds(1))
                    {
                        Id=Guid.NewGuid().ToString(),
                        LimitNumber=20,
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

            IRateLimitStorage storage = new InProcessMemoryStorage();
            if (storageType == "redis")
            {
                storage = new RedisStorage(StackExchange.Redis.ConnectionMultiplexer.Connect("127.0.0.1"));
            }

            return new RateLimitProcessor<SimulationRequest>.Builder()
                .WithAlgorithm(new SlidingWindowAlgorithm<SimulationRequest>(slidingWindowsRules))
                .WithStorage(storage)
                .WithError(new RateLimitError()
                {
                    Code = 429,
                })
                .Build();
        }

        private RateLimitProcessor<SimulationRequest> GetLeakyBucketProcessor(string storageType){
            var leakyBucketRules = new LeakyBucketRateLimitRule<SimulationRequest>[]
                {
                    new LeakyBucketRateLimitRule<SimulationRequest>(30,10,TimeSpan.FromSeconds(1))
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

            IRateLimitStorage storage = new InProcessMemoryStorage();
            if (storageType == "redis")
            {
                storage = new RedisStorage(StackExchange.Redis.ConnectionMultiplexer.Connect("127.0.0.1"));
            }

            return new RateLimitProcessor<SimulationRequest>.Builder()
                .WithAlgorithm(new LeakyBucketAlgorithm<SimulationRequest>(leakyBucketRules))
                .WithStorage(storage)
                .WithError(new RateLimitError()
                {
                    Code = 429,
                })
                .Build();
        }

        private RateLimitProcessor<SimulationRequest> GetTokenBucketProcessor(string storageType){
            var tokenBucketRules = new TokenBucketRateLimitRule<SimulationRequest>[]
                {
                    new TokenBucketRateLimitRule<SimulationRequest>(30,10,TimeSpan.FromSeconds(1))
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

            IRateLimitStorage storage = new InProcessMemoryStorage();
            if (storageType == "redis")
            {
                storage = new RedisStorage(StackExchange.Redis.ConnectionMultiplexer.Connect("127.0.0.1"));
            }

            return new RateLimitProcessor<SimulationRequest>.Builder()
                .WithAlgorithm(new TokenBucketAlgorithm<SimulationRequest>(tokenBucketRules))
                .WithStorage(storage)
                .WithError(new RateLimitError()
                {
                    Code = 429,
                })
                .Build();
        }

        private class SimulationRequest
        {
            public string RequestId { get; set; }

            public string RequestResource { get; set; }

            public Dictionary<string, string> Parameters { get; set; }
        }
    }
}
