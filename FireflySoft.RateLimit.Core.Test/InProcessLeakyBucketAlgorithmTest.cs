using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FireflySoft.RateLimit.Core.InProcessAlgorithm;
using FireflySoft.RateLimit.Core.RedisAlgorithm;
using FireflySoft.RateLimit.Core.Rule;
using FireflySoft.RateLimit.Core.Time;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FireflySoft.RateLimit.Core.Test
{
    [TestClass]
    public class InProcessLeakyBucketAlgorithmTest
    {
        [DataTestMethod]
        public void Common()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, 30, 10, TimeSpan.FromSeconds(1), 1);

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
                    stubTimeProvider.IncrementSeconds(1);
                }
            }
        }

        [DataTestMethod]
        public void ResetTime_NotExistKey_ReturnTimeWindowAsExpireTime()
        {
            var now = DateTimeOffset.Parse("2022-1-1 00:00:00.000");
            var outflowUnit = TimeSpan.FromSeconds(1);
            var stubTimeProvider = new TestTimeProvider(now, TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, 30, 10, outflowUnit, 0);
            var result = processor.Check(new SimulationRequest()
            {
                RequestId = Guid.NewGuid().ToString(),
                RequestResource = "home",
                Parameters = new Dictionary<string, string>() { { "from", "sample" } }
            });

            var expected = now.Add(outflowUnit);
            Assert.AreEqual(expected, result.RuleCheckResults.First().ResetTime);
        }

        [DataTestMethod]
        public void ResetTime_ExistKey_ReturnFirstPeriodExpireTime()
        {
            var now = DateTimeOffset.Parse("2022-1-1 00:00:00.000");
            var outflowUnit = TimeSpan.FromSeconds(1);
            var stubTimeProvider = new TestTimeProvider(now, TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, 30, 10, outflowUnit, 0);

            for (int i = 0; i < 3; i++)
            {
                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                var expected = now.Add(outflowUnit);
                Assert.AreEqual(expected, result.RuleCheckResults.First().ResetTime);

                stubTimeProvider.Increment();
            }
        }

        [DataTestMethod]
        public void ResetTime_ExistKey_ReturnNextPeriodExpireTime()
        {
            var now = DateTimeOffset.Parse("2022-1-1 00:00:00.000");
            var outflowUnit = TimeSpan.FromSeconds(1);
            var stubTimeProvider = new TestTimeProvider(now, TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, 30, 10, outflowUnit, 0);

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
                    var expected = now.Add(outflowUnit);
                    Assert.AreEqual(expected, result.RuleCheckResults.First().ResetTime);
                }

                stubTimeProvider.IncrementMilliseconds(500);
            }
        }

        [DataTestMethod]
        public void ResetTime_TriggerLimit_ReturnLockExpireTime()
        {
            var now = DateTimeOffset.Parse("2022-1-1 00:00:00.000");
            var outflowUnit = TimeSpan.FromMilliseconds(100);
            var stubTimeProvider = new TestTimeProvider(now, TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, 10, 5, outflowUnit, 3);

            for (int i = 0; i < 20; i++)
            {
                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i >= 16)
                {
                    var expected = now.Add(TimeSpan.FromSeconds(3));
                    Assert.AreEqual(expected, result.RuleCheckResults.First().ResetTime);
                }
            }
        }

        [DataTestMethod]
        public void ResetTime_TriggerLimitNoLock_ReturnPeriodExpireTime()
        {
            var now = DateTimeOffset.Parse("2022-1-1 00:00:00.000");
            var outflowUnit = TimeSpan.FromMilliseconds(100);
            var stubTimeProvider = new TestTimeProvider(now, TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, 10, 5, outflowUnit, 0);
            DateTimeOffset expected = DateTimeOffset.MinValue;

            for (int i = 0; i < 20; i++)
            {
                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i == 0)
                {
                    expected = stubTimeProvider.GetCurrentLocalTime().Add(outflowUnit);
                }

                if (i >= 16)
                {
                    Assert.AreEqual(expected, result.RuleCheckResults.First().ResetTime);
                    Assert.AreEqual(expected, result.RuleCheckResults.First().ResetTime);
                }

                stubTimeProvider.Increment();
            }
        }

        [DataTestMethod]
        public void Wait_Common()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, 30, 10, TimeSpan.FromSeconds(1), 0);

            for (int i = 1; i <= 50; i++)
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

                if (i >= 1 && i <= 10)
                {
                    Assert.AreEqual(0, checkResult.RuleCheckResults.First().Wait);
                }

                if (i == 11)
                {
                    Assert.AreEqual(990, checkResult.RuleCheckResults.First().Wait);
                }
                if (i == 12)
                {
                    Assert.AreEqual(989, checkResult.RuleCheckResults.First().Wait);
                }
                if (i == 21)
                {
                    Assert.AreEqual(1980, checkResult.RuleCheckResults.First().Wait);
                }
                if (i == 22)
                {
                    Assert.AreEqual(1979, checkResult.RuleCheckResults.First().Wait);
                }
            }
        }

        [DataTestMethod]
        public void Lock_LockThreeSeconds_Common()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, 30, 10, TimeSpan.FromSeconds(1), 3);

            for (int i = 1; i <= 70; i++)
            {
                stubTimeProvider.Increment();

                if (i == 61 || i == 62 || i == 63)
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
        public void StartTimeType_FromNaturalPeriodBeign_Common()
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
                    stubTimeProvider.IncrementMilliseconds(200);
                }

                if (i == 52)
                {
                    stubTimeProvider.IncrementMilliseconds(1000);
                }
            }
        }

        [DataTestMethod]
        public void StartTimeType_FromCurrent_Common()
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
                    stubTimeProvider.IncrementMilliseconds(200);
                }

                if (i == 52)
                {
                    stubTimeProvider.IncrementMilliseconds(1000);
                }
            }
        }

        [DataTestMethod]
        public void UpdateRules_RaiseCapacity_LoseLimit()
        {
            var ruleId = "UpdateRules_RaiseCapacity_LoseLimit";
            var rule = CreateRules(ruleId, 50, 10, 100);
            IAlgorithm algorithm = new InProcessLeakyBucketAlgorithm(rule, updatable: true);

            for (int i = 1; i <= 80; i++)
            {
                if (i == 71)
                {
                    var newRule = CreateRules(ruleId, 100, 10, 100);
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

                if (i < 61)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }

                if (i >= 61 && i <= 70)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i >= 71)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public void UpdateRules_ReduceCapacity_TriggerLimit()
        {
            var ruleId = "UpdateRules_ReduceCapacity_TriggerLimit";
            var rule = CreateRules(ruleId, 100, 10, 100);
            IAlgorithm algorithm = new InProcessLeakyBucketAlgorithm(rule, updatable: true);

            for (int i = 1; i <= 80; i++)
            {
                if (i == 51)
                {
                    var newRule = CreateRules(ruleId, 40, 10, 100);
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

                if (i > 50)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public void UpdateRules_NarrowOutflowUnit_LoseLimit()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var ruleId = "UpdateRules_NarrowOutflowUnit_LoseLimit";
            var rule = CreateRules(ruleId, 20, 10, 60);
            IAlgorithm algorithm = new InProcessLeakyBucketAlgorithm(rule, stubTimeProvider, updatable: true);

            for (int i = 1; i <= 50; i++)
            {
                if (i == 41)
                {
                    var newRule = CreateRules(ruleId, 20, 10, 40);
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

                if (i < 31)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }

                if (i >= 31 && i <= 40)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                // lose limit
                if (i >= 41)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }

                stubTimeProvider.IncrementMilliseconds(1);
            }
        }

        [DataTestMethod]
        public void UpdateRules_ExpendOutflowUnit_TriggerLimit()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var ruleId = "UpdateRules_ExpendOutflowUnit_TriggerLimit";
            var rule = CreateRules(ruleId, 20, 10, 30);
            IAlgorithm algorithm = new InProcessLeakyBucketAlgorithm(rule, stubTimeProvider, updatable: true);

            for (int i = 1; i <= 50; i++)
            {
                // expend outflow unit
                if (i == 31)
                {
                    var newRule = CreateRules(ruleId, 20, 10, 60);
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

                if (i < 31)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }

                // trigger limit
                if (i == 31)
                {
                    Assert.AreEqual(30, result.RuleCheckResults.First().Count);
                }

                if (i >= 31)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                stubTimeProvider.IncrementMilliseconds(1);
            }
        }

        [DataTestMethod]
        public void UpdateRules_NarrowOutflowQunatity_TriggerLimit()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var ruleId = "UpdateRules_NarrowOutflowQunatity_TriggerLimit";
            var rule = CreateRules(ruleId, 10, 10, 10);
            IAlgorithm algorithm = new InProcessLeakyBucketAlgorithm(rule, stubTimeProvider, updatable: true);

            for (int i = 1; i <= 50; i++)
            {
                if (i == 21)
                {
                    var newRule = CreateRules(ruleId, 10, 5, 10);
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

                //Console.WriteLine($"{i},{result.RuleCheckResults.First().Count},{result.IsLimit}");

                if (i == 20)
                {
                    Assert.AreEqual(10, result.RuleCheckResults.First().Count);
                }

                if (i == 21)
                {
                    Assert.AreEqual(6, result.RuleCheckResults.First().Count);
                }

                if (i == 36)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i == 41)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }

                if (i == 46)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                stubTimeProvider.IncrementMilliseconds(1);
            }
        }

        [DataTestMethod]
        public void UpdateRules_ExpendOutflowQunatity_LoseLimit()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var ruleId = "UpdateRules_ExpendOutflowQunatity_LoseLimit";
            var rule = CreateRules(ruleId, 10, 5, 10);
            IAlgorithm algorithm = new InProcessLeakyBucketAlgorithm(rule, stubTimeProvider, updatable: true);

            for (int i = 1; i <= 50; i++)
            {
                if (i == 27)
                {
                    var newRule = CreateRules(ruleId, 10, 10, 10);
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

                Console.WriteLine($"{i},{result.RuleCheckResults.First().Count},{result.IsLimit}");

                if (i == 11)
                {
                    Assert.AreEqual(6, result.RuleCheckResults.First().Count);
                }

                if (i == 20)
                {
                    Assert.AreEqual(15, result.RuleCheckResults.First().Count);
                }

                if (i == 21)
                {
                    Assert.AreEqual(11, result.RuleCheckResults.First().Count);
                }

                if (i == 26)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                // lose limting
                if (i >= 27)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }

                stubTimeProvider.IncrementMilliseconds(1);
            }
        }

        [DataTestMethod]
        public async Task CommonAsync()
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
                    stubTimeProvider.IncrementMilliseconds(1000);
                }
            }
        }

        [DataTestMethod]
        public async Task ResetTimeAsync_NotExistKey_ReturnTimeWindowAsExpireTime()
        {
            var now = DateTimeOffset.Parse("2022-1-1 00:00:00.000");
            var outflowUnit = TimeSpan.FromSeconds(1);
            var stubTimeProvider = new TestTimeProvider(now, TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, 30, 10, outflowUnit, 0);
            var result = await processor.CheckAsync(new SimulationRequest()
            {
                RequestId = Guid.NewGuid().ToString(),
                RequestResource = "home",
                Parameters = new Dictionary<string, string>() { { "from", "sample" } }
            });

            var expected = now.Add(outflowUnit);
            Assert.AreEqual(expected, result.RuleCheckResults.First().ResetTime);
        }

        [DataTestMethod]
        public async Task ResetTimeAsync_ExistKey_ReturnFirstPeriodExpireTime()
        {
            var now = DateTimeOffset.Parse("2022-1-1 00:00:00.000");
            var outflowUnit = TimeSpan.FromSeconds(1);
            var stubTimeProvider = new TestTimeProvider(now, TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, 30, 10, outflowUnit, 0);

            for (int i = 0; i < 3; i++)
            {
                var result = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                var expected = now.Add(outflowUnit);
                Assert.AreEqual(expected, result.RuleCheckResults.First().ResetTime);

                stubTimeProvider.Increment();
            }
        }

        [DataTestMethod]
        public async Task ResetTimeAsync_ExistKey_ReturnNextPeriodExpireTime()
        {
            var now = DateTimeOffset.Parse("2022-1-1 00:00:00.000");
            var outflowUnit = TimeSpan.FromSeconds(1);
            var stubTimeProvider = new TestTimeProvider(now, TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, 30, 10, outflowUnit, 0);

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
                    var expected = now.Add(outflowUnit);
                    Assert.AreEqual(expected, result.RuleCheckResults.First().ResetTime);
                }

                stubTimeProvider.IncrementMilliseconds(500);
            }
        }

        [DataTestMethod]
        public async Task ResetTimeAsync_TriggerLimit_ReturnLockExpireTime()
        {
            var now = DateTimeOffset.Parse("2022-1-1 00:00:00.000");
            var outflowUnit = TimeSpan.FromMilliseconds(100);
            var stubTimeProvider = new TestTimeProvider(now, TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, 10, 5, outflowUnit, 3);

            for (int i = 0; i < 20; i++)
            {
                var result = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i >= 16)
                {
                    var expected = now.Add(TimeSpan.FromSeconds(3));
                    Assert.AreEqual(expected, result.RuleCheckResults.First().ResetTime);
                }
            }
        }

        [DataTestMethod]
        public async Task WaitAsync_CommonAsync()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, 30, 10, TimeSpan.FromSeconds(1), 0);

            for (int i = 1; i <= 50; i++)
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

                if (i >= 1 && i <= 10)
                {
                    Assert.AreEqual(0, checkResult.RuleCheckResults.First().Wait);
                }

                if (i == 11)
                {
                    Assert.AreEqual(990, checkResult.RuleCheckResults.First().Wait);
                }
                if (i == 12)
                {
                    Assert.AreEqual(989, checkResult.RuleCheckResults.First().Wait);
                }
                if (i == 21)
                {
                    Assert.AreEqual(1980, checkResult.RuleCheckResults.First().Wait);
                }
                if (i == 22)
                {
                    Assert.AreEqual(1979, checkResult.RuleCheckResults.First().Wait);
                }
            }
        }

        [DataTestMethod]
        public async Task LockAsync_LockThreeSeconds_Common()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, 30, 10, TimeSpan.FromSeconds(1), 3);

            for (int i = 1; i <= 70; i++)
            {
                stubTimeProvider.Increment();

                if (i == 61 || i == 62 || i == 63)
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
        public async Task StartTimeTypeAsync_FromNaturalPeriodBeign_Common()
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
                    stubTimeProvider.IncrementMilliseconds(200);
                }

                if (i == 52)
                {
                    stubTimeProvider.IncrementMilliseconds(1000);
                }
            }
        }

        [DataTestMethod]
        public async Task StartTimeTypeAsync_FromCurrent_Common()
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
                    stubTimeProvider.IncrementMilliseconds(200);
                }

                if (i == 52)
                {
                    stubTimeProvider.IncrementMilliseconds(1000);
                }
            }
        }

        [DataTestMethod]
        public async Task UpdateRulesAsync_RaiseCapacity_LoseLimit()
        {
            var ruleId = "UpdateRulesAsync_RaiseCapacity_LoseLimit";
            var rule = CreateRules(ruleId, 50, 10, 100);
            IAlgorithm algorithm = new InProcessLeakyBucketAlgorithm(rule, updatable: true);

            for (int i = 1; i <= 80; i++)
            {
                if (i == 71)
                {
                    var newRule = CreateRules(ruleId, 100, 10, 100);
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

                if (i < 61)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }

                if (i >= 61 && i <= 70)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i >= 71)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public async Task UpdateRulesAsync_ReduceCapacity_TriggerLimit()
        {
            var ruleId = "UpdateRulesAsync_ReduceCapacity_TriggerLimit";
            var rule = CreateRules(ruleId, 100, 10, 100);
            IAlgorithm algorithm = new InProcessLeakyBucketAlgorithm(rule, updatable: true);

            for (int i = 1; i <= 80; i++)
            {
                if (i == 51)
                {
                    var newRule = CreateRules(ruleId, 40, 10, 100);
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

                if (i > 50)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public async Task UpdateRulesAsync_NarrowOutflowUnit_LoseLimit()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var ruleId = "UpdateRulesAsync_NarrowOutflowUnit_LoseLimit";
            var rule = CreateRules(ruleId, 20, 10, 60);
            IAlgorithm algorithm = new InProcessLeakyBucketAlgorithm(rule, stubTimeProvider, updatable: true);

            for (int i = 1; i <= 50; i++)
            {
                if (i == 41)
                {
                    var newRule = CreateRules(ruleId, 20, 10, 40);
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

                if (i < 31)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }

                if (i >= 31 && i <= 40)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                // lose limit
                if (i >= 41)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }

                stubTimeProvider.IncrementMilliseconds(1);
            }
        }

        [DataTestMethod]
        public async Task UpdateRulesAsync_ExpendOutflowUnit_TriggerLimit()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var ruleId = "UpdateRulesAsync_ExpendOutflowUnit_TriggerLimit";
            var rule = CreateRules(ruleId, 20, 10, 30);
            IAlgorithm algorithm = new InProcessLeakyBucketAlgorithm(rule, stubTimeProvider, updatable: true);

            for (int i = 1; i <= 50; i++)
            {
                // expend outflow unit
                if (i == 31)
                {
                    var newRule = CreateRules(ruleId, 20, 10, 60);
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

                if (i < 31)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }

                // trigger limit
                if (i == 31)
                {
                    Assert.AreEqual(30, result.RuleCheckResults.First().Count);
                }

                if (i >= 31)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                stubTimeProvider.IncrementMilliseconds(1);
            }
        }

        [DataTestMethod]
        public async Task UpdateRulesAsync_NarrowOutflowQunatity_TriggerLimit()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var ruleId = "UpdateRulesAsync_NarrowOutflowQunatity_TriggerLimit";
            var rule = CreateRules(ruleId, 10, 10, 10);
            IAlgorithm algorithm = new InProcessLeakyBucketAlgorithm(rule, stubTimeProvider, updatable: true);

            for (int i = 1; i <= 50; i++)
            {
                if (i == 21)
                {
                    var newRule = CreateRules(ruleId, 10, 5, 10);
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

                //Console.WriteLine($"{i},{result.RuleCheckResults.First().Count},{result.IsLimit}");

                if (i == 20)
                {
                    Assert.AreEqual(10, result.RuleCheckResults.First().Count);
                }

                if (i == 21)
                {
                    Assert.AreEqual(6, result.RuleCheckResults.First().Count);
                }

                if (i == 36)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i == 41)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }

                if (i == 46)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                stubTimeProvider.IncrementMilliseconds(1);
            }
        }

        [DataTestMethod]
        public async Task UpdateRulesAsync_ExpendOutflowQunatity_LoseLimit()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var ruleId = "UpdateRulesAsync_ExpendOutflowQunatity_LoseLimit";
            var rule = CreateRules(ruleId, 10, 5, 10);
            IAlgorithm algorithm = new InProcessLeakyBucketAlgorithm(rule, stubTimeProvider, updatable: true);

            for (int i = 1; i <= 50; i++)
            {
                if (i == 27)
                {
                    var newRule = CreateRules(ruleId, 10, 10, 10);
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

                Console.WriteLine($"{i},{result.RuleCheckResults.First().Count},{result.IsLimit}");

                if (i == 11)
                {
                    Assert.AreEqual(6, result.RuleCheckResults.First().Count);
                }

                if (i == 20)
                {
                    Assert.AreEqual(15, result.RuleCheckResults.First().Count);
                }

                if (i == 21)
                {
                    Assert.AreEqual(11, result.RuleCheckResults.First().Count);
                }

                if (i == 26)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                // lose limting
                if (i >= 27)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }

                stubTimeProvider.IncrementMilliseconds(1);
            }
        }

        private static LeakyBucketRule[] CreateRules(string ruleId, int capacity, int outflowQuantity, int outflowUnitMilliseconds)
        {
            return new LeakyBucketRule[]
                {
                    new LeakyBucketRule(capacity,outflowQuantity,TimeSpan.FromMilliseconds(outflowUnitMilliseconds))
                    {
                        Id=ruleId,
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

        private IAlgorithm GetAlgorithm(ITimeProvider timeProvider, int capacity, int outflowQuantity, TimeSpan outflowUnit, int lockSeconds, StartTimeType startTimeType = StartTimeType.FromCurrent, string id = "")
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

            return new InProcessLeakyBucketAlgorithm(leakyBucketRules, timeProvider);
        }
    }
}