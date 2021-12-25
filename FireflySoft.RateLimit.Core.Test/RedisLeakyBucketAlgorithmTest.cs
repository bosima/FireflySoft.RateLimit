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
    public class RedisLeakyBucketAlgorithmTest
    {
        [DataTestMethod]
        public void Test()
        {
            var processor = GetAlgorithm(30, 10, TimeSpan.FromSeconds(1), 1);

            for (int i = 1; i <= 80; i++)
            {
                var checkResult = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i == 41 || i >= 52)
                {
                    Assert.AreEqual(true, checkResult.IsLimit);
                }
                else
                {
                    Assert.AreEqual(false, checkResult.IsLimit);
                }

                if (i == 41)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        [DataTestMethod]
        public void TestLockSeconds()
        {
            var processor = GetAlgorithm(30, 10, TimeSpan.FromSeconds(1), 3);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 61 || i == 62 || i == 63)
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

                if (i > 40 && i <= 62)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i <= 40 || i > 62)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public async Task TestLockSecondsAsync()
        {
            var processor = GetAlgorithm(30, 10, TimeSpan.FromSeconds(1), 3);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 61 || i == 62 || i == 63)
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

                if (i > 40 && i <= 62)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i <= 40 || i > 62)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public void TestFromNaturalPeriodBeign()
        {
            var processor = GetAlgorithm(30, 10, TimeSpan.FromSeconds(1), 0, StartTimeType.FromNaturalPeriodBeign);

            while (true)
            {
                if (DateTimeOffset.Now.Millisecond < 800)
                {
                    Thread.Sleep(20);
                    continue;
                }
                break;
            }

            for (int i = 1; i <= 80; i++)
            {
                var checkResult = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i == 41 || i == 52 || i >= 63)
                {
                    Assert.AreEqual(true, checkResult.IsLimit);
                }
                else
                {
                    Assert.AreEqual(false, checkResult.IsLimit);
                }

                if (i == 41)
                {
                    Thread.Sleep(200);
                }

                if (i == 52)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        [DataTestMethod]


        public void TestFromCurrent()
        {
            var processor = GetAlgorithm(30, 10, TimeSpan.FromSeconds(1), 0, StartTimeType.FromCurrent);

            while (true)
            {
                if (DateTimeOffset.Now.Millisecond < 800)
                {
                    Thread.Sleep(20);
                    continue;
                }
                break;
            }

            for (int i = 1; i <= 80; i++)
            {
                var checkResult = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if ((i >= 41 && i <= 52) || i >= 63)
                {
                    Assert.AreEqual(true, checkResult.IsLimit);
                }
                else
                {
                    Assert.AreEqual(false, checkResult.IsLimit);
                }

                if (i == 41)
                {
                    Thread.Sleep(200);
                }

                if (i == 52)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        [DataTestMethod]
        public async Task TestAsync()
        {
            var processor = GetAlgorithm(30, 10, TimeSpan.FromSeconds(1), 1);

            for (int i = 1; i <= 80; i++)
            {
                var checkResult = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i == 41 || i >= 52)
                {
                    Assert.AreEqual(true, checkResult.IsLimit);
                }
                else
                {
                    Assert.AreEqual(false, checkResult.IsLimit);
                }

                if (i == 41)
                {
                    await Task.Delay(1000);
                }
            }
        }

        [DataTestMethod]


        public async Task TestFromNaturalPeriodBeignAsync()
        {
            var processor = GetAlgorithm(30, 10, TimeSpan.FromSeconds(1), 0, StartTimeType.FromNaturalPeriodBeign);

            while (true)
            {
                if (DateTimeOffset.Now.Millisecond < 800)
                {
                    Thread.Sleep(20);
                    continue;
                }
                break;
            }

            for (int i = 1; i <= 80; i++)
            {
                var checkResult = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i == 41 || i == 52 || i >= 63)
                {
                    Assert.AreEqual(true, checkResult.IsLimit);
                }
                else
                {
                    Assert.AreEqual(false, checkResult.IsLimit);
                }

                if (i == 41)
                {
                    Thread.Sleep(200);
                }

                if (i == 52)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        [DataTestMethod]


        public async Task TestFromCurrentAsync()
        {
            var processor = GetAlgorithm(30, 10, TimeSpan.FromSeconds(1), 0, StartTimeType.FromCurrent);

            while (true)
            {
                if (DateTimeOffset.Now.Millisecond < 800)
                {
                    Thread.Sleep(20);
                    continue;
                }
                break;
            }

            for (int i = 1; i <= 80; i++)
            {
                var checkResult = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if ((i >= 41 && i <= 52) || i >= 63)
                {
                    Assert.AreEqual(true, checkResult.IsLimit);
                }
                else
                {
                    Assert.AreEqual(false, checkResult.IsLimit);
                }

                if (i == 41)
                {
                    Thread.Sleep(200);
                }

                if (i == 52)
                {
                    Thread.Sleep(1000);
                }
            }
        }


        [DataTestMethod]

        public void TestRedisKeyExpire()
        {
            var processor = GetAlgorithm(30, 10, TimeSpan.FromSeconds(1), 0, id: "1");

            for (int i = 1; i <= 90; i++)
            {
                var checkResult = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i == 41 || i >= 82)
                {
                    Assert.AreEqual(true, checkResult.IsLimit);
                }
                else
                {
                    Assert.AreEqual(false, checkResult.IsLimit);
                }

                if (i == 41)
                {
                    SpinWait.SpinUntil(() => { return false; }, 3000);

                    var redisClient = RedisClientHelper.GetClient();
                    bool exsit = redisClient.GetDatabase().KeyExists("1-home-st");
                    Assert.AreEqual(false, exsit);
                }
            }
        }

        private IAlgorithm GetAlgorithm(int capacity, int outflowQuantity, TimeSpan outflowUnit, int lockSeconds, StartTimeType startTimeType = StartTimeType.FromCurrent, string id = "")
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                id = Guid.NewGuid().ToString();
            }

            var leakyBucketRules = new LeakyBucketRule[]
                {
                    new LeakyBucketRule(capacity,outflowQuantity,outflowUnit)
                    {
                        Id=id,
                        LockSeconds=lockSeconds,
                        StartTimeType=startTimeType,
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

            var redisClient = RedisClientHelper.GetClient();
            return new RedisLeakyBucketAlgorithm(leakyBucketRules, redisClient);
        }
    }
}