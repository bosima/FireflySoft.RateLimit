using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FireflySoft.RateLimit.Core.UnitTest
{
    [TestClass]
    public class SlidingWindowAlgorithmTest
    {
        [DataTestMethod]
        [DataRow("memory")]
        [DataRow("redis")]
        public void Test(string storageType)
        {
            var processor = GetSlidingWindowProcessor(storageType, TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(1), lockSeconds: 0);

            for (int i = 1; i <= 15; i++)
            {
                int j = 3;
                if (i == 7 || i == 10)
                {
                    j = 6;
                }

                Debug.WriteLine("for " + i);

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

                if (i == 7 || i == 10)
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
        public void TestLockSeconds(string storageType)
        {
            var processor = GetSlidingWindowProcessor(storageType, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(100), StartTimeType.FromCurrent, 20, 3);

            for (int i = 1; i <= 40; i++)
            {
                if (i >= 22 && i <= 24)
                {
                    Thread.Sleep(1001);
                }

                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i >= 21 && i <= 23)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }
                else
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        [DataRow("memory")]
        [DataRow("redis")]
        public async Task TestAsync(string storageType)
        {
            var processor = GetSlidingWindowProcessor(storageType, TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(1), lockSeconds: 0);

            for (int i = 1; i <= 15; i++)
            {
                int j = 3;
                if (i == 7 || i == 10)
                {
                    j = 6;
                }
                Debug.WriteLine("for " + i);

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

                if (i == 7 || i == 10)
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
        public void TestFromNaturalPeriodBeign(string storageType)
        {
            var processor = GetSlidingWindowProcessor(storageType, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(100), StartTimeType.FromNaturalPeriodBeign, 20, 0);

            while (true)
            {
                if (DateTimeOffset.Now.Millisecond < 800)
                {
                    Thread.Sleep(100);
                    continue;
                }
                break;
            }

            for (int i = 1; i <= 30; i++)
            {
                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i > 20 && i <= 25)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i <= 20 || i > 25)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }

                if (i == 25)
                {
                    Thread.Sleep(1001);
                }
            }
        }

        [DataTestMethod]
        [DataRow("memory")]
        [DataRow("redis")]
        public void TestFromCurrent(string storageType)
        {
            var processor = GetSlidingWindowProcessor(storageType, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(100), StartTimeType.FromNaturalPeriodBeign, 20, 0);

            while (true)
            {
                if (DateTimeOffset.Now.Millisecond < 800)
                {
                    Thread.Sleep(100);
                    continue;
                }
                break;
            }

            for (int i = 1; i <= 30; i++)
            {
                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i > 20 && i <= 25)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i <= 20 || i > 25)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }

                if (i == 25)
                {
                    Thread.Sleep(1001);
                }
            }
        }

        [DataTestMethod]
        [DataRow("memory")]
        [DataRow("redis")]
        public async Task TestFromNaturalPeriodBeignAsync(string storageType)
        {
            var processor = GetSlidingWindowProcessor(storageType, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(100), StartTimeType.FromNaturalPeriodBeign, 20, 0);

            while (true)
            {
                if (DateTimeOffset.Now.Millisecond < 800)
                {
                    Thread.Sleep(100);
                    continue;
                }
                break;
            }

            for (int i = 1; i <= 30; i++)
            {
                var result = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i > 20 && i <= 25)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i <= 20 || i > 25)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }

                if (i == 25)
                {
                    Thread.Sleep(1001);
                }
            }
        }

        [DataTestMethod]
        [DataRow("memory")]
        [DataRow("redis")]
        public async Task TestFromCurrentAsync(string storageType)
        {
            var processor = GetSlidingWindowProcessor(storageType, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(100), StartTimeType.FromNaturalPeriodBeign, 20, 0);

            while (true)
            {
                if (DateTimeOffset.Now.Millisecond < 800)
                {
                    Thread.Sleep(100);
                    continue;
                }
                break;
            }

            for (int i = 1; i <= 30; i++)
            {
                var result = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i > 20 && i <= 25)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i <= 20 || i > 25)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }

                if (i == 25)
                {
                    Thread.Sleep(1001);
                }
            }
        }

        private RateLimitProcessor<SimulationRequest> GetSlidingWindowProcessor(string storageType, TimeSpan statWindow, TimeSpan statPeriod, StartTimeType startTimeType = StartTimeType.FromCurrent, int limitNumber = 20, int lockSeconds = 1)
        {
            var slidingWindowsRules = new SlidingWindowRateLimitRule<SimulationRequest>[]
                {
                    new SlidingWindowRateLimitRule<SimulationRequest>(statWindow, statPeriod)
                    {
                        Id=Guid.NewGuid().ToString(),
                        LimitNumber=limitNumber,
                        LockSeconds=lockSeconds,
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
    }
}