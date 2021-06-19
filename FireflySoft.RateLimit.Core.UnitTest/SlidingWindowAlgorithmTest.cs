using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FireflySoft.RateLimit.Core.InProcessAlgorithm;
using FireflySoft.RateLimit.Core.RedisAlgorithm;
using FireflySoft.RateLimit.Core.Rule;
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
            var processor = GetAlgorithm(storageType, TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(1), lockSeconds: 0);

            for (int i = 1; i <= 15; i++)
            {
                int j = 3;
                if (i == 7 || i == 10)
                {
                    j = 6;
                }

                AlgorithmCheckResult checkResult = null;
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
                    Debug.WriteLine(DateTimeOffset.Now.ToString("HH:mm:ss.fff"));
                }

                Debug.WriteLine(i + ":" + checkResult.IsLimit + "," + checkResult.RuleCheckResults.First().Count);

                if (i == 7 || i == 10)
                {
                    Assert.AreEqual(true, checkResult.IsLimit);
                }
                else
                {
                    Assert.AreEqual(false, checkResult.IsLimit);
                }

                Task.Delay(1000).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [DataTestMethod]
        [DataRow("memory")]
        [DataRow("redis")]
        public async Task TestAsync(string storageType)
        {
            var processor = GetAlgorithm(storageType, TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(1), lockSeconds: 0);

            for (int i = 1; i <= 15; i++)
            {
                int j = 3;
                if (i == 7 || i == 10)
                {
                    j = 6;
                }

                AlgorithmCheckResult checkResult = null;
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
                    Debug.WriteLine(DateTimeOffset.Now.ToString("HH:mm:ss.fff"));
                }

                Debug.WriteLine("for " + i + "," + checkResult.RuleCheckResults.First().Count);
                if (i == 7 || i == 10)
                {
                    Assert.AreEqual(true, checkResult.IsLimit);
                }
                else
                {
                    Assert.AreEqual(false, checkResult.IsLimit);
                }

                await Task.Delay(1000);
            }
        }

        [DataTestMethod]
        [DataRow("memory")]
        [DataRow("redis")]
        public void TestLockSeconds(string storageType)
        {
            var processor = GetAlgorithm(storageType, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(100), StartTimeType.FromCurrent, 20, 3);

            for (int i = 1; i <= 40; i++)
            {
                if (i >= 22 && i <= 24)
                {
                    Thread.Sleep(1100);
                }

                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                Debug.WriteLine(DateTimeOffset.Now.ToString("mm:ss.fff") + " for " + i + "," + result.IsLimit + "," + result.RuleCheckResults.First().Count);

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
        public async Task TestLockSecondsAsync(string storageType)
        {
            var processor = GetAlgorithm(storageType, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(100), StartTimeType.FromCurrent, 20, 3);

            for (int i = 1; i <= 40; i++)
            {
                if (i >= 22 && i <= 24)
                {
                    await Task.Delay(1000);
                }

                var result = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                Debug.WriteLine(DateTimeOffset.Now.ToString("mm:ss.fff") + " for " + i + "," + result.RuleCheckResults.First().Count);

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
        public void TestFromNaturalPeriodBeign(string storageType)
        {
            var processor = GetAlgorithm(storageType, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(100), StartTimeType.FromNaturalPeriodBeign, 20, 0);

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
            var processor = GetAlgorithm(storageType, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(100), StartTimeType.FromNaturalPeriodBeign, 20, 0);

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
            var processor = GetAlgorithm(storageType, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(100), StartTimeType.FromNaturalPeriodBeign, 20, 0);

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
                var checkResult = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i > 20 && i <= 25)
                {
                    Assert.AreEqual(true, checkResult.IsLimit);
                }

                if (i <= 20 || i > 25)
                {
                    Assert.AreEqual(false, checkResult.IsLimit);
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
            var processor = GetAlgorithm(storageType, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(100), StartTimeType.FromNaturalPeriodBeign, 20, 0);

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
                var checkResult = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i > 20 && i <= 25)
                {
                    Assert.AreEqual(true, checkResult.IsLimit);
                }

                if (i <= 20 || i > 25)
                {
                    Assert.AreEqual(false, checkResult.IsLimit);
                }

                if (i == 25)
                {
                    Thread.Sleep(1001);
                }
            }
        }

        [DataTestMethod]
        [DataRow("redis")]
        public void TestRedisKeyExpire(string storageType)
        {
            var processor = GetAlgorithm(storageType, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1), lockSeconds: 0, id: "1");

            for (int i = 1; i <= 15; i++)
            {
                int j = 6;
                if (i == 4 || i == 8)
                {
                    j = 12;
                }

                AlgorithmCheckResult checkResult = null;
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
                    Debug.WriteLine(DateTimeOffset.Now.ToString("HH:mm:ss.fff"));
                }

                Debug.WriteLine(i + ":" + checkResult.IsLimit + "," + checkResult.RuleCheckResults.First().Count);

                if (i == 4 || i == 8)
                {
                    Assert.AreEqual(true, checkResult.IsLimit);
                    SpinWait.SpinUntil(() => { return false; }, 3000);

                    var redisClient = GetRedisClient();
                    bool exsit = redisClient.GetDatabase().KeyExists("1-home-st");
                    Assert.AreEqual(false, exsit);
                }
                else
                {
                    Assert.AreEqual(false, checkResult.IsLimit);
                    SpinWait.SpinUntil(() => { return false; }, 1000);
                }
            }
        }

        private IAlgorithm GetAlgorithm(string storageType, TimeSpan statWindow, TimeSpan statPeriod, StartTimeType startTimeType = StartTimeType.FromCurrent, int limitNumber = 20, int lockSeconds = 1, string id = "")
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                id = Guid.NewGuid().ToString();
            }

            var slidingWindowsRules = new SlidingWindowRule[]
                {
                    new SlidingWindowRule(statWindow, statPeriod)
                    {
                        Id=id,
                        LimitNumber=limitNumber,
                        LockSeconds=lockSeconds,
                        ExtractTarget = (request) =>
                        {
                            return (request as SimulationRequest).RequestResource;
                        },
                        CheckRuleMatching = (request) =>
                        {
                            return true;
                        },
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

            if (storageType == "redis")
            {
                var redisClient = GetRedisClient();
                return new RedisSlidingWindowAlgorithm(slidingWindowsRules, redisClient);
            }
            else
            {
                return new InProcessSlidingWindowAlgorithm(slidingWindowsRules);
            }
        }

        private StackExchange.Redis.ConnectionMultiplexer _redisClient;
        private StackExchange.Redis.ConnectionMultiplexer GetRedisClient()
        {
            _redisClient = StackExchange.Redis.ConnectionMultiplexer.Connect("127.0.0.1");
            return _redisClient;
        }
    }
}