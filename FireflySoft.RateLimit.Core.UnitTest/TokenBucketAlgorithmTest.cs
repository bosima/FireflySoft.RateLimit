using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FireflySoft.RateLimit.Core.UnitTest
{
    [TestClass]
    public class TokenBucketAlgorithmTest
    {
        [ClassInitialize]
        public static void Init(TestContext tc)
        {
        }

        [DataTestMethod]
        [DataRow("memory")]
        [DataRow("redis")]
        public void Test(string storageType)
        {
            var processor = GetTokenBucketProcessor(storageType, 30, 10, TimeSpan.FromSeconds(1), 1);

            for (int i = 1; i <= 80; i++)
            {
                Debug.WriteLine("for " + i);

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
        public void TestLockSeconds(string storageType)
        {
            var processor = GetTokenBucketProcessor(storageType, 30, 10, TimeSpan.FromSeconds(1), 3);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 61 && i == 62 && i == 63)
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

                if (i > 30 && i <= 62)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i <= 50 && i > 62)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        [DataRow("memory")]
        [DataRow("redis")]
        public void TestFromNaturalPeriodBeign(string storageType)
        {
            var processor = GetTokenBucketProcessor(storageType, 30, 10, TimeSpan.FromSeconds(1), 0, StartTimeType.FromNaturalPeriodBeign);

            while (true)
            {
                if (DateTimeOffset.Now.Millisecond < 800)
                {
                    Thread.Sleep(50);
                    continue;
                }
                break;
            }

            for (int i = 1; i <= 80; i++)
            {
                Debug.WriteLine("for " + i);

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

                if (i == 31)
                {
                    Thread.Sleep(200);
                }

                if (i == 42)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        [DataTestMethod]
        [DataRow("memory")]
        [DataRow("redis")]
        public void TestFromCurrent(string storageType)
        {
            var processor = GetTokenBucketProcessor(storageType, 30, 10, TimeSpan.FromSeconds(1), 0, StartTimeType.FromCurrent);

            while (true)
            {
                if (DateTimeOffset.Now.Millisecond < 800)
                {
                    Thread.Sleep(50);
                    continue;
                }
                break;
            }

            for (int i = 1; i <= 80; i++)
            {
                Debug.WriteLine("for " + i);

                var checkResult = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if ((i >= 31 && i <= 42) || i >= 53)
                {
                    Assert.AreEqual(true, checkResult.IsLimit);
                }
                else
                {
                    Assert.AreEqual(false, checkResult.IsLimit);
                }

                if (i == 31)
                {
                    Thread.Sleep(200);
                }

                if (i == 42)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        [DataTestMethod]
        [DataRow("memory")]
        [DataRow("redis")]
        public async Task TestAsync(string storageType)
        {
            var processor = GetTokenBucketProcessor(storageType, 30, 10, TimeSpan.FromSeconds(1), 1);

            for (int i = 1; i <= 80; i++)
            {
                Debug.WriteLine("for " + i);

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

        [DataTestMethod]
        [DataRow("memory")]
        [DataRow("redis")]
        public async Task TestFromNaturalPeriodBeignAsync(string storageType)
        {
            var processor = GetTokenBucketProcessor(storageType, 30, 10, TimeSpan.FromSeconds(1), 0, StartTimeType.FromNaturalPeriodBeign);

            while (true)
            {
                if (DateTimeOffset.Now.Millisecond < 800)
                {
                    Thread.Sleep(50);
                    continue;
                }
                break;
            }

            for (int i = 1; i <= 80; i++)
            {
                Debug.WriteLine("for " + i);

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

                if (i == 31)
                {
                    Thread.Sleep(200);
                }

                if (i == 42)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        [DataTestMethod]
        [DataRow("memory")]
        [DataRow("redis")]
        public async Task TestFromCurrentAsync(string storageType)
        {
            var processor = GetTokenBucketProcessor(storageType, 30, 10, TimeSpan.FromSeconds(1), 0, StartTimeType.FromCurrent);

            while (true)
            {
                if (DateTimeOffset.Now.Millisecond < 800)
                {
                    Thread.Sleep(50);
                    continue;
                }
                break;
            }

            for (int i = 1; i <= 80; i++)
            {
                Debug.WriteLine("for " + i);

                var checkResult = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if ((i >= 31 && i <= 42) || i >= 53)
                {
                    Assert.AreEqual(true, checkResult.IsLimit);
                }
                else
                {
                    Assert.AreEqual(false, checkResult.IsLimit);
                }

                if (i == 31)
                {
                    Thread.Sleep(200);
                }

                if (i == 42)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        private RateLimitProcessor<SimulationRequest> GetTokenBucketProcessor(string storageType, int capacity, int inflowQuantity, TimeSpan inflowUnit, int lockSeconds, StartTimeType startTimeType = StartTimeType.FromCurrent)
        {
            var tokenBucketRules = new TokenBucketRateLimitRule<SimulationRequest>[]
                {
                    new TokenBucketRateLimitRule<SimulationRequest>(capacity,inflowQuantity,inflowUnit)
                    {
                        Id=Guid.NewGuid().ToString(),
                        LockSeconds=lockSeconds,
                        StartTimeType=startTimeType,
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
    }
}
