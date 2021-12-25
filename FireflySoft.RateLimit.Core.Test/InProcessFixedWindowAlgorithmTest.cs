using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FireflySoft.RateLimit.Core.InProcessAlgorithm;
using FireflySoft.RateLimit.Core.Rule;
using FireflySoft.RateLimit.Core.Time;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FireflySoft.RateLimit.Core.Test
{
    [TestClass]
    public class InProcessFixedWindowAlgorithmTest
    {
        [DataTestMethod]
        public void Test()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, TimeSpan.FromMilliseconds(60), StartTimeType.FromCurrent, 50, 0);

            // 0-50 51-60 61-110 111-120 121-140
            for (int i = 1; i <= 140; i++)
            {
                stubTimeProvider.Increment();

                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if ((i >= 51 && i <= 60) || (i >= 111 && i <= 120))
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i <= 50 || (i >= 61 && i <= 110) || i >= 121)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public void TestFromNaturalPeriodBeign()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, TimeSpan.FromSeconds(1), StartTimeType.FromNaturalPeriodBeign, 50, 0);

            stubTimeProvider.IncrementMilliseconds(800);

            // 1-50 51-55 56-70
            for (int i = 1; i <= 70; i++)
            {
                stubTimeProvider.Increment();

                if (i == 56)
                {
                    stubTimeProvider.IncrementMilliseconds(200);
                }

                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i >= 51 && i <= 55)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i <= 50 || i >= 56)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public void TestFromCurrent()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, TimeSpan.FromSeconds(1), StartTimeType.FromCurrent, 50, 0);

            stubTimeProvider.IncrementMilliseconds(800);

            for (int i = 1; i <= 100; i++)
            {
                stubTimeProvider.Increment();

                if (i == 56)
                {
                    stubTimeProvider.IncrementMilliseconds(200);
                }

                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i >= 51 && i <= 70)
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
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, TimeSpan.FromMilliseconds(60), StartTimeType.FromCurrent, 50, 0);

            // 0-50 51-60 61-110 111-120 121-140
            for (int i = 1; i <= 140; i++)
            {
                stubTimeProvider.Increment();

                var result = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if ((i >= 51 && i <= 60) || (i >= 111 && i <= 120))
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i <= 50 || (i >= 61 && i <= 110) || i >= 121)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public async Task TestFromNaturalPeriodBeignAsync()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, TimeSpan.FromSeconds(1), StartTimeType.FromNaturalPeriodBeign, 50, 0);

            stubTimeProvider.IncrementMilliseconds(800);

            // 1-50 51-55 56-70
            for (int i = 1; i <= 70; i++)
            {
                stubTimeProvider.Increment();

                if (i == 56)
                {
                    stubTimeProvider.IncrementMilliseconds(200);
                }

                var result = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i >= 51 && i <= 55)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i <= 50 || i >= 56)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public async Task TestFromCurrentAsync()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, TimeSpan.FromSeconds(1), StartTimeType.FromCurrent, 50, 0);

            stubTimeProvider.IncrementMilliseconds(800);

            for (int i = 1; i <= 100; i++)
            {
                stubTimeProvider.Increment();

                if (i == 56)
                {
                    stubTimeProvider.IncrementMilliseconds(200);
                }

                var result = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i >= 51 && i <= 70)
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
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, TimeSpan.FromSeconds(1), StartTimeType.FromNaturalPeriodBeign, 50, 3);

            for (int i = 1; i <= 70; i++)
            {
                stubTimeProvider.Increment();

                if (i == 61 || i == 62 || i == 63)
                {
                    stubTimeProvider.IncrementMilliseconds(1000);
                }

                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i >= 51 && i <= 62)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i <= 50 || i >= 63)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public async Task TestLockSecondsAsync()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, TimeSpan.FromSeconds(1), StartTimeType.FromNaturalPeriodBeign, 50, 3);

            for (int i = 1; i <= 70; i++)
            {
                stubTimeProvider.Increment();

                if (i == 61 || i == 62 || i == 63)
                {
                    stubTimeProvider.IncrementMilliseconds(1000);
                }

                var result = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i >= 51 && i <= 62)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i <= 50 || i >= 63)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        private IAlgorithm GetAlgorithm(ITimeProvider timeProvider, TimeSpan statWindow, StartTimeType startTimeType, int limitNumber, int lockSeconds)
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

            return new InProcessFixedWindowAlgorithm(fixedWindowRules, timeProvider);
        }
    }
}