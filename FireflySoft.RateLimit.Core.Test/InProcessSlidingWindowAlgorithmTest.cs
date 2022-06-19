using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FireflySoft.RateLimit.Core.InProcessAlgorithm;
using FireflySoft.RateLimit.Core.Rule;
using FireflySoft.RateLimit.Core.Time;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FireflySoft.RateLimit.Core.Test
{
    [TestClass]
    public class InProcessSlidingWindowAlgorithmTest
    {
        [DataTestMethod]
        public void Common()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(1), lockSeconds: 0);

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

                Console.WriteLine($"{checkResult.IsLimit},{checkResult.RuleCheckResults.First().Count}");

                if (i == 7 || i == 10)
                {
                    Assert.AreEqual(true, checkResult.IsLimit);
                }
                else
                {
                    Assert.AreEqual(false, checkResult.IsLimit);
                }

                stubTimeProvider.IncrementSeconds(1);
            }
        }

        [DataTestMethod]
        public void ResetTime_NotExistKey_ReturnTimeWindowAsExpireTime()
        {
            var now = DateTimeOffset.Parse("2022-1-1 00:00:00.000");
            var statWindow = TimeSpan.FromSeconds(3);
            var statPeriod = TimeSpan.FromSeconds(1);
            var stubTimeProvider = new TestTimeProvider(now, TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, statWindow, statPeriod, StartTimeType.FromCurrent, 10, 0);
            var result = processor.Check(new SimulationRequest()
            {
                RequestId = Guid.NewGuid().ToString(),
                RequestResource = "home",
                Parameters = new Dictionary<string, string>() { { "from", "sample" } }
            });

            var expected = now.Add(statPeriod);
            Assert.AreEqual(expected, result.RuleCheckResults.First().ResetTime);
        }

        [DataTestMethod]
        public void ResetTime_ExistKey_ReturnFirstPeriodExpireTime()
        {
            var now = DateTimeOffset.Parse("2022-1-1 00:00:00.000");
            var statWindow = TimeSpan.FromSeconds(3);
            var statPeriod = TimeSpan.FromSeconds(1);
            var stubTimeProvider = new TestTimeProvider(now, TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, statWindow, statPeriod, StartTimeType.FromCurrent, 10, 0);

            for (int i = 0; i < 3; i++)
            {
                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                var expected = now.Add(statPeriod);
                Assert.AreEqual(expected, result.RuleCheckResults.First().ResetTime);

                stubTimeProvider.Increment();
            }
        }

        [DataTestMethod]
        public void ResetTime_ExistKey_ReturnNextPeriodExpireTime()
        {
            var now = DateTimeOffset.Parse("2022-1-1 00:00:00.000");
            var statWindow = TimeSpan.FromSeconds(3);
            var statPeriod = TimeSpan.FromSeconds(1);
            var stubTimeProvider = new TestTimeProvider(now, TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, statWindow, statPeriod, StartTimeType.FromCurrent, 10, 0);

            for (int i = 0; i < 3; i++)
            {
                if (i == 2)
                {
                    now = stubTimeProvider.GetCurrentLocalTime();
                }

                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i == 2)
                {
                    var expected = now.Add(statPeriod);
                    Assert.AreEqual(expected, result.RuleCheckResults.First().ResetTime);
                }

                stubTimeProvider.IncrementMilliseconds(500);
            }
        }

        [DataTestMethod]
        public void ResetTime_TriggerLimit_ReturnLockExpireTime()
        {
            var now = DateTimeOffset.Parse("2022-1-1 00:00:00.000");
            var statWindow = TimeSpan.FromMilliseconds(300);
            var statPeriod = TimeSpan.FromMilliseconds(100);
            var stubTimeProvider = new TestTimeProvider(now, TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, statWindow, statPeriod, StartTimeType.FromCurrent, 10, 3);

            for (int i = 0; i < 10; i++)
            {
                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i >= 11)
                {
                    var expected = now.Add(TimeSpan.FromSeconds(3));
                    Assert.AreEqual(expected, result.RuleCheckResults.First().ResetTime);
                }
            }
        }

        [DataTestMethod]
        public void Lock_LockThreeSeconds_Common()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(100), StartTimeType.FromCurrent, 20, 3);

            for (int i = 1; i <= 40; i++)
            {
                stubTimeProvider.Increment();
                if (i >= 22 && i <= 24)
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
        public void StartTimeType_FromNaturalPeriodBeign_Common()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(100), StartTimeType.FromNaturalPeriodBeign, 20, 0);

            stubTimeProvider.IncrementMilliseconds(800);

            for (int i = 1; i <= 30; i++)
            {
                stubTimeProvider.Increment();

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
                    stubTimeProvider.IncrementSeconds(1);
                }
            }
        }

        [DataTestMethod]
        public void StartTimeType_FromCurrent_Common()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(100), StartTimeType.FromNaturalPeriodBeign, 20, 0);

            stubTimeProvider.IncrementMilliseconds(800);

            for (int i = 1; i <= 30; i++)
            {
                stubTimeProvider.Increment();

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
                    stubTimeProvider.IncrementSeconds(1);
                }
            }
        }

        [DataTestMethod]
        public void UpdateRules_RaiseLimitNumber_LoseLimit()
        {
            var ruleId = "UpdateRules_RaiseLimitNumber_LoseLimit";
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            SlidingWindowRule[] rule = CreateRules(50, ruleId, 1000, 100);

            IAlgorithm algorithm = new InProcessSlidingWindowAlgorithm(rule, stubTimeProvider, updatable: true);

            for (int i = 1; i <= 80; i++)
            {
                if (i == 61)
                {
                    var newRule = CreateRules(60, ruleId, 1000, 100);
                    algorithm.UpdateRules(newRule);
                }

                var result = algorithm.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i <= 50)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }

                if (i > 50 && i <= 60)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                // lose limit
                if (i >= 61 && i <= 70)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }

                if (i > 70)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public void UpdateRules_ReduceLimitNumber_TriggerLimit()
        {
            var ruleId = "UpdateRules_ReduceLimitNumber_TriggerLimit";
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var rule = CreateRules(50, ruleId, 1000, 100);

            IAlgorithm algorithm = new InProcessSlidingWindowAlgorithm(rule, stubTimeProvider, updatable: true);

            for (int i = 1; i <= 80; i++)
            {
                if (i == 41)
                {
                    var newRule = CreateRules(40, ruleId, 1000, 100);
                    algorithm.UpdateRules(newRule);
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
        public void UpdateRules_NarrowTimeWindowAndExpired_StartNewTimeWindow()
        {
            var ruleId = "UpdateRules_NarrowTimeWindowAndExpired_StartNewTimeWindow";
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var rule = CreateRules(50, ruleId, 1000, 100);

            IAlgorithm algorithm = new InProcessSlidingWindowAlgorithm(rule, stubTimeProvider, updatable: true);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 52)
                {
                    var newRule = CreateRules(50, ruleId, 500, 100);
                    algorithm.UpdateRules(newRule);
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

                // new period
                if (i == 52)
                {
                    Assert.AreEqual(1, result.RuleCheckResults.First().Count);
                }

                if (i < 51 || i > 51)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public void UpdateRules_NarrowTimeWindowAndNotExpired_KeepLimit()
        {
            var ruleId = "UpdateRules_NarrowTimeWindowAndNotExpired_KeepLimit";
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var rule = CreateRules(50, ruleId, 1000, 100);

            IAlgorithm algorithm = new InProcessSlidingWindowAlgorithm(rule, stubTimeProvider, updatable: true);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 52)
                {
                    var newRule = CreateRules(50, ruleId, 500, 100);
                    algorithm.UpdateRules(newRule);
                    stubTimeProvider.IncrementMilliseconds(100);
                }

                var result = algorithm.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i >= 51)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i < 51)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public void UpdateRules_NarrowTimeWindowAndPartialExpired_LoseLimit()
        {
            var ruleId = "UpdateRules_NarrowTimeWindowAndPartialExpired_LoseLimit";
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var rule = CreateRules(100, ruleId, 1000, 100);

            IAlgorithm algorithm = new InProcessSlidingWindowAlgorithm(rule, stubTimeProvider, updatable: true);

            int[] increamentNumber = new int[] { 11, 21, 31, 41, 51, 61, 71, 81, 91 };

            for (int i = 1; i <= 120; i++)
            {
                if (increamentNumber.Contains(i))
                {
                    stubTimeProvider.IncrementMilliseconds(100);
                }

                if (i == 102)
                {
                    var newRule = CreateRules(100, ruleId, 500, 100);
                    algorithm.UpdateRules(newRule);
                    stubTimeProvider.IncrementMilliseconds(100);
                }

                var result = algorithm.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i == 101)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i == 120)
                {
                    Assert.AreEqual(59, result.RuleCheckResults.First().Count);
                }

                if (i < 101 && i > 101)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public void UpdateRules_ExpendTimeWindowNotExpired_Keeplimit()
        {
            var ruleId = "UpdateRulesAsync_ExpendTimeWindowNotExpired_Keeplimit";
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var rule = CreateRules(50, ruleId, 1000, 100);

            IAlgorithm algorithm = new InProcessSlidingWindowAlgorithm(rule, stubTimeProvider, updatable: true);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 52)
                {
                    var fixedWindowRules2 = CreateRules(50, ruleId, 2000, 100);
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
        public void UpdateRules_ExpendTimeWindowNotExpired_Triggerlimit()
        {
            var ruleId = "UpdateRules_ExpendTimeWindowNotExpired_Triggerlimit";
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var rule = CreateRules(120, ruleId, 1000, 100);

            IAlgorithm algorithm = new InProcessSlidingWindowAlgorithm(rule, stubTimeProvider, updatable: true);

            int[] increamentNumber = new int[] { 11, 21, 31, 41, 51, 61, 71, 81, 91, 101, 111, 121, 131 };

            for (int i = 1; i <= 140; i++)
            {
                if (increamentNumber.Contains(i))
                {
                    stubTimeProvider.IncrementMilliseconds(100);
                }

                if (i == 102)
                {
                    var newRule = CreateRules(120, ruleId, 2000, 100);
                    algorithm.UpdateRules(newRule);
                    stubTimeProvider.IncrementMilliseconds(100);
                }

                var result = algorithm.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i == 101)
                {
                    Assert.AreEqual(91, result.RuleCheckResults.First().Count);
                }

                if (i == 111)
                {
                    Assert.AreEqual(101, result.RuleCheckResults.First().Count);
                }

                if (i == 121)
                {
                    Assert.AreEqual(111, result.RuleCheckResults.First().Count);
                }

                if (i >= 131)
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
        public void UpdateRules_ExpendTimeWindowNotExpired_Nolimit()
        {
            var ruleId = "UpdateRules_ExpendTimeWindowNotExpired_Nolimit";
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var rule = CreateRules(90, ruleId, 1000, 100);

            IAlgorithm algorithm = new InProcessSlidingWindowAlgorithm(rule, stubTimeProvider, updatable: true);

            int[] increamentNumber = new int[] { 11, 21, 31, 41, 51, 61, 71, 81, 91, 101, 111 };

            for (int i = 1; i <= 120; i++)
            {
                if (increamentNumber.Contains(i))
                {
                    stubTimeProvider.IncrementMilliseconds(300);
                }
                if (i == 21)
                {
                    var newRule = CreateRules(90, ruleId, 2000, 100);
                    algorithm.UpdateRules(newRule);
                }

                var result = algorithm.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                Assert.AreEqual(false, result.IsLimit);
            }
        }

        [DataTestMethod]
        public void UpdateRules_ChangeStatPeriod_CreateNewTimeWindow()
        {
            var ruleId = "UpdateRules_ChangeStatPeriod_CreateNewTimeWindow";
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var rule = CreateRules(100, ruleId, 1000, 100);

            IAlgorithm algorithm = new InProcessSlidingWindowAlgorithm(rule, stubTimeProvider, updatable: true);

            for (int i = 1; i <= 120; i++)
            {
                if (i == 100)
                {
                    var newRule = CreateRules(100, ruleId, 1000, 50);
                    algorithm.UpdateRules(newRule);
                }

                var result = algorithm.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i == 100)
                {
                    Assert.AreEqual(1, result.RuleCheckResults.First().Count);
                }

                Assert.AreEqual(false, result.IsLimit);
            }
        }

        [DataTestMethod]
        public async Task CommonAsync()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(1), lockSeconds: 0);

            for (int i = 1; i <= 15; i++)
            {
                stubTimeProvider.Increment();

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

                stubTimeProvider.IncrementSeconds(1);
            }
        }

        [DataTestMethod]
        public async Task ResetTimeAsync_NotExistKey_ReturnTimeWindowAsExpireTime()
        {
            var now = DateTimeOffset.Parse("2022-1-1 00:00:00.000");
            var statWindow = TimeSpan.FromSeconds(3);
            var statPeriod = TimeSpan.FromSeconds(1);
            var stubTimeProvider = new TestTimeProvider(now, TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, statWindow, statPeriod, StartTimeType.FromCurrent, 10, 0);
            var result = await processor.CheckAsync(new SimulationRequest()
            {
                RequestId = Guid.NewGuid().ToString(),
                RequestResource = "home",
                Parameters = new Dictionary<string, string>() { { "from", "sample" } }
            });

            var expected = now.Add(statPeriod);
            Assert.AreEqual(expected, result.RuleCheckResults.First().ResetTime);
        }

        [DataTestMethod]
        public async Task ResetTimeAsync_ExistKey_ReturnFirstPeriodExpireTime()
        {
            var now = DateTimeOffset.Parse("2022-1-1 00:00:00.000");
            var statWindow = TimeSpan.FromSeconds(3);
            var statPeriod = TimeSpan.FromSeconds(1);
            var stubTimeProvider = new TestTimeProvider(now, TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, statWindow, statPeriod, StartTimeType.FromCurrent, 10, 0);

            for (int i = 0; i < 3; i++)
            {
                var result = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                var expected = now.Add(statPeriod);
                Assert.AreEqual(expected, result.RuleCheckResults.First().ResetTime);

                stubTimeProvider.Increment();
            }
        }

        [DataTestMethod]
        public async Task ResetTimeAsync_ExistKey_ReturnNextPeriodExpireTime()
        {
            var now = DateTimeOffset.Parse("2022-1-1 00:00:00.000");
            var statWindow = TimeSpan.FromSeconds(3);
            var statPeriod = TimeSpan.FromSeconds(1);
            var stubTimeProvider = new TestTimeProvider(now, TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, statWindow, statPeriod, StartTimeType.FromCurrent, 10, 0);

            for (int i = 0; i < 3; i++)
            {
                if (i == 2)
                {
                    now = stubTimeProvider.GetCurrentLocalTime();
                }

                var result = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i == 2)
                {
                    var expected = now.Add(statPeriod);
                    Assert.AreEqual(expected, result.RuleCheckResults.First().ResetTime);
                }

                stubTimeProvider.IncrementMilliseconds(500);
            }
        }

        [DataTestMethod]
        public async Task ResetTimeAsync_TriggerLimit_ReturnLockExpireTime()
        {
            var now = DateTimeOffset.Parse("2022-1-1 00:00:00.000");
            var statWindow = TimeSpan.FromMilliseconds(300);
            var statPeriod = TimeSpan.FromMilliseconds(100);
            var stubTimeProvider = new TestTimeProvider(now, TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, statWindow, statPeriod, StartTimeType.FromCurrent, 10, 3);

            for (int i = 0; i < 10; i++)
            {
                var result = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i >= 11)
                {
                    var expected = now.Add(TimeSpan.FromSeconds(3));
                    Assert.AreEqual(expected, result.RuleCheckResults.First().ResetTime);
                }
            }
        }

        [DataTestMethod]
        public async Task LockAsync_LockThreeSeconds_Common()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(100), StartTimeType.FromCurrent, 20, 3);

            for (int i = 1; i <= 40; i++)
            {
                stubTimeProvider.Increment();

                if (i >= 22 && i <= 24)
                {
                    stubTimeProvider.IncrementSeconds(1);
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
        public async Task StartTimeTypeAsync_FromNaturalPeriodBeign_Common()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(100), StartTimeType.FromNaturalPeriodBeign, 20, 0);

            stubTimeProvider.IncrementMilliseconds(800);

            for (int i = 1; i <= 30; i++)
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
                    stubTimeProvider.IncrementSeconds(1);
                }
            }
        }

        [DataTestMethod]
        public async Task StartTimeTypeAsync_FromCurrent_Common()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(100), StartTimeType.FromNaturalPeriodBeign, 20, 0);

            stubTimeProvider.IncrementMilliseconds(800);

            for (int i = 1; i <= 30; i++)
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
                    stubTimeProvider.IncrementSeconds(1);
                }
            }
        }

        [DataTestMethod]
        public async Task UpdateRulesAsync_RaiseLimitNumber_LoseLimit()
        {
            var ruleId = "UpdateRulesAsync_RaiseLimitNumber_LoseLimit";
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var rule = CreateRules(50, ruleId, 1000, 100);

            IAlgorithm algorithm = new InProcessSlidingWindowAlgorithm(rule, stubTimeProvider, updatable: true);

            for (int i = 1; i <= 80; i++)
            {
                if (i == 61)
                {
                    var newRule = CreateRules(60, ruleId, 1000, 100);
                    await algorithm.UpdateRulesAsync(newRule);
                }

                var result = await algorithm.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i <= 50)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }

                if (i > 50 && i <= 60)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                // lose limit
                if (i >= 61 && i <= 70)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }

                if (i > 70)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public async Task UpdateRulesAsync_ReduceLimitNumber_TriggerLimit()
        {
            var ruleId = "UpdateRulesAsync_ReduceLimitNumber_TriggerLimit";
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var rule = CreateRules(50, ruleId, 1000, 100);

            IAlgorithm algorithm = new InProcessSlidingWindowAlgorithm(rule, stubTimeProvider, updatable: true);

            for (int i = 1; i <= 80; i++)
            {
                if (i == 41)
                {
                    var newRule = CreateRules(40, ruleId, 1000, 100);
                    algorithm.UpdateRules(newRule);
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
        public async Task UpdateRulesAsync_NarrowTimeWindowAndExpired_StartNewTimeWindow()
        {
            var ruleId = "UpdateRulesAsync_NarrowTimeWindowAndExpired_StartNewTimeWindow";
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var rule = CreateRules(50, ruleId, 1000, 100);

            IAlgorithm algorithm = new InProcessSlidingWindowAlgorithm(rule, stubTimeProvider, updatable: true);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 52)
                {
                    var newRule = CreateRules(50, ruleId, 500, 100);
                    await algorithm.UpdateRulesAsync(newRule);
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

                // new period
                if (i == 52)
                {
                    Assert.AreEqual(1, result.RuleCheckResults.First().Count);
                }

                if (i < 51 || i > 51)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public async Task UpdateRulesAsync_NarrowTimeWindowAndNotExpired_KeepLimit()
        {
            var ruleId = "UpdateRulesAsync_NarrowTimeWindowAndNotExpired_KeepLimit";
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var rule = CreateRules(50, ruleId, 1000, 100);

            IAlgorithm algorithm = new InProcessSlidingWindowAlgorithm(rule, stubTimeProvider, updatable: true);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 52)
                {
                    var newRule = CreateRules(50, ruleId, 500, 100);
                    await algorithm.UpdateRulesAsync(newRule);
                    stubTimeProvider.IncrementMilliseconds(100);
                }

                var result = await algorithm.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i >= 51)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i < 51)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public async Task UpdateRulesAsync_NarrowTimeWindowAndPartialExpired_LoseLimit()
        {
            var ruleId = "UpdateRulesAsync_NarrowTimeWindowAndPartialExpired_LoseLimit";
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var rule = CreateRules(100, ruleId, 1000, 100);

            IAlgorithm algorithm = new InProcessSlidingWindowAlgorithm(rule, stubTimeProvider, updatable: true);

            int[] increamentNumber = new int[] { 11, 21, 31, 41, 51, 61, 71, 81, 91 };

            for (int i = 1; i <= 120; i++)
            {
                if (increamentNumber.Contains(i))
                {
                    stubTimeProvider.IncrementMilliseconds(100);
                }

                if (i == 102)
                {
                    var newRule = CreateRules(100, ruleId, 500, 100);
                    await algorithm.UpdateRulesAsync(newRule);
                    stubTimeProvider.IncrementMilliseconds(100);
                }

                var result = await algorithm.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i == 101)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i == 120)
                {
                    Assert.AreEqual(59, result.RuleCheckResults.First().Count);
                }

                if (i < 101 && i > 101)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public async Task UpdateRulesAsync_ExpendTimeWindowNotExpired_Keeplimit()
        {
            var ruleId = "UpdateRulesAsync_ExpendTimeWindowNotExpired_Keeplimit";
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var rule = CreateRules(50, ruleId, 1000, 100);

            IAlgorithm algorithm = new InProcessSlidingWindowAlgorithm(rule, stubTimeProvider, updatable: true);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 52)
                {
                    var fixedWindowRules2 = CreateRules(50, ruleId, 2000, 100);
                    await algorithm.UpdateRulesAsync(fixedWindowRules2);
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
        public async Task UpdateRulesAsync_ExpendTimeWindowNotExpired_Triggerlimit()
        {
            var ruleId = "UpdateRulesAsync_ExpendTimeWindowNotExpired_Triggerlimit";
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var rule = CreateRules(120, ruleId, 1000, 100);

            IAlgorithm algorithm = new InProcessSlidingWindowAlgorithm(rule, stubTimeProvider, updatable: true);

            int[] increamentNumber = new int[] { 11, 21, 31, 41, 51, 61, 71, 81, 91, 101, 111, 121, 131 };

            for (int i = 1; i <= 140; i++)
            {
                if (increamentNumber.Contains(i))
                {
                    stubTimeProvider.IncrementMilliseconds(100);
                }

                if (i == 102)
                {
                    var newRule = CreateRules(120, ruleId, 2000, 100);
                    await algorithm.UpdateRulesAsync(newRule);
                    stubTimeProvider.IncrementMilliseconds(100);
                }

                var result = await algorithm.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i == 101)
                {
                    Assert.AreEqual(91, result.RuleCheckResults.First().Count);
                }

                if (i == 111)
                {
                    Assert.AreEqual(101, result.RuleCheckResults.First().Count);
                }

                if (i == 121)
                {
                    Assert.AreEqual(111, result.RuleCheckResults.First().Count);
                }

                if (i >= 131)
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
        public async Task UpdateRulesAsync_ExpendTimeWindowNotExpired_Nolimit()
        {
            var ruleId = "UpdateRulesAsync_ExpendTimeWindowNotExpired_Nolimit";
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var rule = CreateRules(90, ruleId, 1000, 100);

            IAlgorithm algorithm = new InProcessSlidingWindowAlgorithm(rule, stubTimeProvider, updatable: true);

            int[] increamentNumber = new int[] { 11, 21, 31, 41, 51, 61, 71, 81, 91, 101, 111 };

            for (int i = 1; i <= 120; i++)
            {
                if (increamentNumber.Contains(i))
                {
                    stubTimeProvider.IncrementMilliseconds(300);
                }
                if (i == 21)
                {
                    var newRule = CreateRules(90, ruleId, 2000, 100);
                    await algorithm.UpdateRulesAsync(newRule);
                }

                var result = await algorithm.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                Assert.AreEqual(false, result.IsLimit);
            }
        }

        [DataTestMethod]
        public async Task UpdateRulesAsync_ChangeStatPeriod_CreateNewTimeWindow()
        {
            var ruleId = "UpdateRulesAsync_ChangeStatPeriod_CreateNewTimeWindow";
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var rule = CreateRules(100, ruleId, 1000, 100);

            IAlgorithm algorithm = new InProcessSlidingWindowAlgorithm(rule, stubTimeProvider, updatable: true);

            for (int i = 1; i <= 120; i++)
            {
                if (i == 100)
                {
                    var newRule = CreateRules(100, ruleId, 1000, 50);
                    await algorithm.UpdateRulesAsync(newRule);
                }

                var result = await algorithm.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i == 100)
                {
                    Assert.AreEqual(1, result.RuleCheckResults.First().Count);
                }

                Assert.AreEqual(false, result.IsLimit);
            }
        }

        private static SlidingWindowRule[] CreateRules(int limitNumber, string ruleId, long statWindowMilliseconds = 1000, long statPeriodMilliseconds = 100)
        {
            return new SlidingWindowRule[]
                {
                    new SlidingWindowRule(TimeSpan.FromMilliseconds(statWindowMilliseconds), TimeSpan.FromMilliseconds(statPeriodMilliseconds))
                    {
                        Id=ruleId,
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

        private IAlgorithm GetAlgorithm(ITimeProvider timeProvider, TimeSpan statWindow, TimeSpan statPeriod, StartTimeType startTimeType = StartTimeType.FromCurrent, int limitNumber = 20, int lockSeconds = 1, string id = "")
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


            return new InProcessSlidingWindowAlgorithm(slidingWindowsRules, timeProvider);
        }
    }
}