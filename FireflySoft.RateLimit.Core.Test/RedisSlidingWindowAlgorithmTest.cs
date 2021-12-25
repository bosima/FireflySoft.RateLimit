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

namespace FireflySoft.RateLimit.Core.Test
{
    [TestClass]
    public class RedisSlidingWindowAlgorithmTest
    {
        [DataTestMethod]
        public void Test()
        {
            var processor = GetAlgorithm(TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(1), lockSeconds: 0);

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
        public async Task TestAsync()
        {
            var processor = GetAlgorithm(TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(1), lockSeconds: 0);

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
                }

                // Requests that are limited are not counted
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
        public void TestLockSeconds()
        {
            var processor = GetAlgorithm(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(100), StartTimeType.FromCurrent, 20, 3);

            for (int i = 1; i <= 40; i++)
            {
                if (i >= 22 && i <= 24)
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
        public async Task TestLockSecondsAsync()
        {
            var processor = GetAlgorithm(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(100), StartTimeType.FromCurrent, 20, 3);

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
        public void TestFromNaturalPeriodBeign()
        {
            var processor = GetAlgorithm(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(100), StartTimeType.FromNaturalPeriodBeign, 20, 0);

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
                    Thread.Sleep(1000);
                }
            }
        }

        [DataTestMethod]
        public void TestFromCurrent()
        {
            var processor = GetAlgorithm(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(100), StartTimeType.FromNaturalPeriodBeign, 20, 0);

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
                    Thread.Sleep(1000);
                }
            }
        }

        [DataTestMethod]
        public async Task TestFromNaturalPeriodBeignAsync()
        {
            var processor = GetAlgorithm(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(100), StartTimeType.FromNaturalPeriodBeign, 20, 0);

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
                    await Task.Delay(1000);
                }
            }
        }

        [DataTestMethod]
        public async Task TestFromCurrentAsync()
        {
            var processor = GetAlgorithm(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(100), StartTimeType.FromNaturalPeriodBeign, 20, 0);

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
                    await Task.Delay(1000);
                }
            }
        }

        [DataTestMethod]
        public void TestRedisKeyExpire()
        {
            var processor = GetAlgorithm(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1), lockSeconds: 0, id: "1");

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
                }

                if (i == 4 || i == 8)
                {
                    Assert.AreEqual(true, checkResult.IsLimit);
                    SpinWait.SpinUntil(() => { return false; }, 3000);

                    var redisClient = RedisClientHelper.GetClient();
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

        private IAlgorithm GetAlgorithm(TimeSpan statWindow, TimeSpan statPeriod, StartTimeType startTimeType = StartTimeType.FromCurrent, int limitNumber = 20, int lockSeconds = 1, string id = "")
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

            var redisClient = RedisClientHelper.GetClient();
            return new RedisSlidingWindowAlgorithm(slidingWindowsRules, redisClient);
        }
    }
}