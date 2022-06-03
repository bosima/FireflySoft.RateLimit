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

        [DataTestMethod]
        public void UpdateRules_RaiseLimitNumber_NoLimit()
        {
            FixedWindowRule[] fixedWindowRules = CreateRules(50);

            IAlgorithm algorithm = new InProcessFixedWindowAlgorithm(fixedWindowRules, updatable: true);

            for (int i = 1; i <= 80; i++)
            {
                if (i == 61)
                {
                    var fixedWindowRules2 = CreateRules(60);
                    algorithm.UpdateRules(fixedWindowRules2);
                }

                var result = algorithm.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if ((i > 50 && i <= 60) || i > 70)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i <= 50 || (i >= 61 && i <= 70))
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public void UpdateRules_ReduceLimitNumber_NoLimit()
        {
            FixedWindowRule[] fixedWindowRules = CreateRules(50);

            IAlgorithm algorithm = new InProcessFixedWindowAlgorithm(fixedWindowRules, updatable: true);

            for (int i = 1; i <= 80; i++)
            {
                if (i == 41)
                {
                    var fixedWindowRules2 = CreateRules(40);
                    algorithm.UpdateRules(fixedWindowRules2);
                }

                var result = algorithm.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i > 40)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i <= 40)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public async Task UpdateRulesAsync_RaiseLimitNumber_NoLimit()
        {
            FixedWindowRule[] fixedWindowRules = CreateRules(50);

            IAlgorithm algorithm = new InProcessFixedWindowAlgorithm(fixedWindowRules, updatable: true);

            for (int i = 1; i <= 80; i++)
            {
                if (i == 61)
                {
                    var fixedWindowRules2 = CreateRules(60);
                    await algorithm.UpdateRulesAsync(fixedWindowRules2);
                }

                var result = await algorithm.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if ((i > 50 && i <= 60) || i > 70)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i <= 50 || (i >= 61 && i <= 70))
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public async Task UpdateRulesAsync_ReduceLimitNumber_NoLimit()
        {
            FixedWindowRule[] fixedWindowRules = CreateRules(50);

            IAlgorithm algorithm = new InProcessFixedWindowAlgorithm(fixedWindowRules, updatable: true);

            for (int i = 1; i <= 80; i++)
            {
                if (i == 41)
                {
                    var fixedWindowRules2 = CreateRules(40);
                    algorithm.UpdateRules(fixedWindowRules2);
                }

                var result = await algorithm.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i > 40)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i <= 40)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public void UpdateRules_NarrowTimeWindowExpired_CreateNewTimeWindow()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            FixedWindowRule[] fixedWindowRules = CreateRules(50, 1000);

            IAlgorithm algorithm = new InProcessFixedWindowAlgorithm(fixedWindowRules, stubTimeProvider, updatable: true);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 52)
                {
                    var fixedWindowRules2 = CreateRules(50, 500);
                    algorithm.UpdateRules(fixedWindowRules2);
                    stubTimeProvider.IncrementMilliseconds(500);
                }

                var result = algorithm.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i == 51)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i < 51 || i > 51)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public void UpdateRules_NarrowTimeWindowNotExpired_ResizeTimeWindow()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            FixedWindowRule[] fixedWindowRules = CreateRules(50, 1000);

            IAlgorithm algorithm = new InProcessFixedWindowAlgorithm(fixedWindowRules,stubTimeProvider, updatable: true);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 52)
                {
                    var fixedWindowRules2 = CreateRules(50, 500);
                    algorithm.UpdateRules(fixedWindowRules2);
                }

                if (i == 61)
                {
                    stubTimeProvider.IncrementMilliseconds(200);
                }

                var result = algorithm.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i == 52)
                {
                    stubTimeProvider.IncrementMilliseconds(300);
                }

                if (i >= 51 && i < 61)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i < 51 || i >= 61)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public void UpdateRules_ExpendTimeWindowNotExpired_ResizeTimeWindow()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            FixedWindowRule[] fixedWindowRules = CreateRules(50, 1000);

            IAlgorithm algorithm= new InProcessFixedWindowAlgorithm(fixedWindowRules, stubTimeProvider,updatable: true);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 52)
                {
                    var fixedWindowRules2 = CreateRules(50, 2000);
                    algorithm.UpdateRules(fixedWindowRules2);
                }

                if (i == 61)
                {
                    stubTimeProvider.IncrementMilliseconds(1000);
                }

                var result = algorithm.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i == 52)
                {
                     stubTimeProvider.IncrementMilliseconds(1000);
                }

                //Console.WriteLine(result.RuleCheckResults.First().Count);
                if (i >= 51 && i < 61)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i < 51 || i >= 61)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public async Task UpdateRulesAsync_NarrowTimeWindowExpired_CreateNewTimeWindow()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            FixedWindowRule[] fixedWindowRules = CreateRules(50, 1000);

            IAlgorithm algorithm= new InProcessFixedWindowAlgorithm(fixedWindowRules, stubTimeProvider,updatable: true);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 52)
                {
                    var fixedWindowRules2 = CreateRules(50, 500);
                    algorithm.UpdateRules(fixedWindowRules2);
                    stubTimeProvider.IncrementMilliseconds(500);
                }

                var result = await algorithm.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i == 51)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i < 51 || i > 51)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public async Task UpdateRulesAsync_NarrowTimeWindowNotExpired_ResizeTimeWindow()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            FixedWindowRule[] fixedWindowRules = CreateRules(50, 1000);

            IAlgorithm algorithm = new InProcessFixedWindowAlgorithm(fixedWindowRules,stubTimeProvider, updatable: true);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 52)
                {
                    var fixedWindowRules2 = CreateRules(50, 500);
                    algorithm.UpdateRules(fixedWindowRules2);
                }

                if (i == 61)
                {
                    stubTimeProvider.IncrementMilliseconds(200);
                }

                var result = await algorithm.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i == 52)
                {
                    stubTimeProvider.IncrementMilliseconds(300);
                }

                if (i >= 51 && i < 61)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i < 51 || i >= 61)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public async Task UpdateRulesAsync_ExpendTimeWindowNotExpired_ResizeTimeWindow()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            FixedWindowRule[] fixedWindowRules = CreateRules(50, 1000);

            IAlgorithm algorithm= new InProcessFixedWindowAlgorithm(fixedWindowRules,stubTimeProvider, updatable: true);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 52)
                {
                    var fixedWindowRules2 = CreateRules(50, 2000);
                    algorithm.UpdateRules(fixedWindowRules2);
                }

                if (i == 61)
                {
                    stubTimeProvider.IncrementMilliseconds(1000);
                }

                var result = await algorithm.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i == 52)
                {
                    stubTimeProvider.IncrementMilliseconds(1000);
                }

                //Console.WriteLine(result.RuleCheckResults.First().Count);
                if (i >= 51 && i < 61)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i < 51 || i >= 61)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        private static FixedWindowRule[] CreateRules(int limitNumber, long statWindowMilliseconds = 1000)
        {
            return new FixedWindowRule[]
                {
                    new FixedWindowRule()
                    {
                        Id="TestUpdateRules-1",
                        StatWindow=TimeSpan.FromMilliseconds(statWindowMilliseconds),
                        LimitNumber=limitNumber,
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