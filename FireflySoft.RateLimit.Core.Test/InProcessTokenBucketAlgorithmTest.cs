using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using FireflySoft.RateLimit.Core.Rule;
using FireflySoft.RateLimit.Core.InProcessAlgorithm;
using FireflySoft.RateLimit.Core.RedisAlgorithm;
using FireflySoft.RateLimit.Core.Time;

namespace FireflySoft.RateLimit.Core.Test
{
    [TestClass]
    public class InProcessTokenBucketAlgorithmTest
    {
        [DataTestMethod]
        public void Test()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, 30, 10, TimeSpan.FromSeconds(1), 0);

            for (int i = 1; i <= 80; i++)
            {
                stubTimeProvider.Increment();

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
                    stubTimeProvider.IncrementMilliseconds(1000);
                }
            }
        }

        [DataTestMethod]
        public void TestLockSeconds()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, 30, 10, TimeSpan.FromSeconds(1), 3);

            for (int i = 1; i <= 70; i++)
            {
                stubTimeProvider.Increment();

                if (i == 61 && i == 62 && i == 63)
                {
                    stubTimeProvider.IncrementSeconds(1);
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
        public void TestFromNaturalPeriodBeign()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, 30, 10, TimeSpan.FromSeconds(1), 0, StartTimeType.FromNaturalPeriodBeign);

            stubTimeProvider.IncrementMilliseconds(800);

            for (int i = 1; i <= 80; i++)
            {
                stubTimeProvider.Increment();

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
                    stubTimeProvider.IncrementMilliseconds(200);
                }

                if (i == 42)
                {
                    stubTimeProvider.IncrementMilliseconds(1000);
                }
            }
        }

        [DataTestMethod]
        public void TestFromCurrent()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, 30, 10, TimeSpan.FromSeconds(1), 0, StartTimeType.FromCurrent);

            stubTimeProvider.IncrementMilliseconds(800);

            for (int i = 1; i <= 80; i++)
            {
                stubTimeProvider.Increment();

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
                    stubTimeProvider.IncrementMilliseconds(200);
                }

                if (i == 42)
                {
                    stubTimeProvider.IncrementMilliseconds(1000);
                }
            }
        }

        [DataTestMethod]
        public async Task TestAsync()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, 30, 10, TimeSpan.FromSeconds(1), 1);

            for (int i = 1; i <= 80; i++)
            {
                stubTimeProvider.Increment();

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
                    stubTimeProvider.IncrementMilliseconds(1000);
                }
            }
        }

        [DataTestMethod]
        public async Task TestLockSecondsAsync()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, 30, 10, TimeSpan.FromSeconds(1), 3);

            for (int i = 1; i <= 70; i++)
            {
                stubTimeProvider.Increment();

                if (i == 61 && i == 62 && i == 63)
                {
                    stubTimeProvider.IncrementMilliseconds(1000);
                }
                var result = await processor.CheckAsync(new SimulationRequest()
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
        public async Task TestFromNaturalPeriodBeignAsync()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, 30, 10, TimeSpan.FromSeconds(1), 0, StartTimeType.FromNaturalPeriodBeign);

            stubTimeProvider.IncrementMilliseconds(800);

            for (int i = 1; i <= 80; i++)
            {
                stubTimeProvider.Increment();

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
                    stubTimeProvider.IncrementMilliseconds(200);
                }

                if (i == 42)
                {
                    stubTimeProvider.IncrementMilliseconds(1000);
                }
            }
        }

        [DataTestMethod]
        public async Task TestFromCurrentAsync()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, 30, 10, TimeSpan.FromSeconds(1), 0, StartTimeType.FromCurrent);

            stubTimeProvider.IncrementMilliseconds(800);

            for (int i = 1; i <= 80; i++)
            {
                stubTimeProvider.Increment();

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
                    stubTimeProvider.IncrementMilliseconds(200);
                }

                if (i == 42)
                {
                    stubTimeProvider.IncrementMilliseconds(1000);
                }
            }
        }

        private IAlgorithm GetAlgorithm(ITimeProvider timeProvider, int capacity, int inflowQuantity, TimeSpan inflowUnit, int lockSeconds, StartTimeType startTimeType = StartTimeType.FromCurrent, string id = "")
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                id = Guid.NewGuid().ToString();
            }

            var tokenBucketRules = new TokenBucketRule[]
                {
                    new TokenBucketRule(capacity,inflowQuantity,inflowUnit)
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

            return new InProcessTokenBucketAlgorithm(tokenBucketRules, timeProvider);
        }
    }
}
