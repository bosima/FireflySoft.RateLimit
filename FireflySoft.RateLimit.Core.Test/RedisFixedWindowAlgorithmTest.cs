using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FireflySoft.RateLimit.Core.InProcessAlgorithm;
using FireflySoft.RateLimit.Core.RedisAlgorithm;
using FireflySoft.RateLimit.Core.Rule;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FireflySoft.RateLimit.Core.Test
{
    [TestClass]
    public class RedisFixedWindowAlgorithmTest
    {
        [DataTestMethod]
        public void Test()
        {
            var processor = GetAlgorithm(TimeSpan.FromSeconds(1), StartTimeType.FromCurrent, 50, 0);

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

                if (i <= 50 || i > 60)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public void TestFromNaturalPeriodBeign()
        {
            var processor = GetAlgorithm(TimeSpan.FromSeconds(1), StartTimeType.FromNaturalPeriodBeign, 50, 0);

            while (true)
            {
                if (DateTimeOffset.Now.Millisecond < 800)
                {
                    Thread.Sleep(50);
                    continue;
                }
                break;
            }

            for (int i = 1; i <= 70; i++)
            {
                if (i == 56)
                {
                    Thread.Sleep(200);
                }
                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i > 50 && i <= 55)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i <= 50 || i > 55)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public void TestFromCurrent()
        {
            var processor = GetAlgorithm(TimeSpan.FromSeconds(1), StartTimeType.FromCurrent, 50, 0);

            while (true)
            {
                if (DateTimeOffset.Now.Millisecond < 900)
                {
                    Thread.Sleep(100);
                    continue;
                }
                break;
            }

            for (int i = 1; i <= 70; i++)
            {
                if (i == 56)
                {
                    Thread.Sleep(100);
                }
                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i > 50 && i <= 70)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i <= 50)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public async Task TestAsync()
        {
            var processor = GetAlgorithm(TimeSpan.FromSeconds(1), StartTimeType.FromCurrent, 50, 0);

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

                if (i <= 50 || i > 60)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public async Task TestFromNaturalPeriodBeignAsync()
        {
            var processor = GetAlgorithm(TimeSpan.FromSeconds(1), StartTimeType.FromNaturalPeriodBeign, 50, 0);

            while (true)
            {
                if (DateTimeOffset.Now.Millisecond < 800)
                {
                    Thread.Sleep(100);
                    continue;
                }
                break;
            }

            for (int i = 1; i <= 70; i++)
            {
                if (i == 56)
                {
                    Thread.Sleep(200);
                }
                var result = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i > 50 && i <= 55)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i <= 50 || i > 55)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public async Task TestFromCurrentAsync()
        {
            var processor = GetAlgorithm(TimeSpan.FromSeconds(1), StartTimeType.FromCurrent, 50, 0);

            while (true)
            {
                if (DateTimeOffset.Now.Millisecond < 900)
                {
                    Thread.Sleep(100);
                    continue;
                }
                break;
            }

            for (int i = 1; i <= 70; i++)
            {
                if (i == 56)
                {
                    Thread.Sleep(100);
                }
                var result = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i > 50 && i <= 70)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i <= 50)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public void TestLockSeconds()
        {
            var processor = GetAlgorithm(TimeSpan.FromSeconds(1), StartTimeType.FromCurrent, 50, 3);

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

                Console.WriteLine(i + ":" + result.IsLimit);

                if (i > 50 && i <= 62)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i <= 50 || i > 62)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public async Task TestLockSecondsAsync()
        {
            var processor = GetAlgorithm(TimeSpan.FromSeconds(1), StartTimeType.FromCurrent, 50, 3);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 61 || i == 62 || i == 63)
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

                Console.WriteLine(i + ":" + result.IsLimit);

                if (i > 50 && i <= 62)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i <= 50 || i > 62)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        private IAlgorithm GetAlgorithm(TimeSpan statWindow, StartTimeType startTimeType, int limitNumber, int lockSeconds)
        {
            var fixedWindowRules = new FixedWindowRule[]
                {
                    new FixedWindowRule()
                    {
                        Id=Guid.NewGuid().ToString(),
                        StatWindow=statWindow,
                        StartTimeType=startTimeType,
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
            return new RedisFixedWindowAlgorithm(fixedWindowRules, redisClient);
        }
    }
}