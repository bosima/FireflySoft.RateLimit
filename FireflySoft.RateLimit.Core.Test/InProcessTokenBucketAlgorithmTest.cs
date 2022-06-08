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
using System.Linq;

namespace FireflySoft.RateLimit.Core.Test
{
    [TestClass]
    public class InProcessTokenBucketAlgorithmTest
    {
        [DataTestMethod]
        public void Common()
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
        public void Lock_LockThreeSeconds_Common()
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
        public void UpdateRules_RaiseCapacity_KeepLimit()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var ruleId = "UpdateRules_RaiseCapacity_KeepLimit";
            var rule = CreateRules(ruleId, 50, 10, 100);
            IAlgorithm algorithm = new InProcessTokenBucketAlgorithm(rule, stubTimeProvider, updatable: true);

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

                if (i < 51)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }

                if (i >= 51)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                stubTimeProvider.Increment();
            }
        }

        [DataTestMethod]
        public void UpdateRules_ReduceCapacity_TriggerLimit()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var ruleId = "UpdateRules_ReduceCapacity_TriggerLimit";
            var rule = CreateRules(ruleId, 100, 10, 100);
            IAlgorithm algorithm = new InProcessTokenBucketAlgorithm(rule, stubTimeProvider, updatable: true);

            for (int i = 1; i <= 80; i++)
            {
                if (i == 21)
                {
                    var newRule = CreateRules(ruleId, 30, 10, 100);
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

                if (i < 51)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }

                if (i >= 51)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                stubTimeProvider.Increment();
            }
        }

        [DataTestMethod]
        public void UpdateRules_NarrowInflowUnit_LoseLimit()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var ruleId = "UpdateRules_NarrowInflowUnit_LoseLimit";
            var rule = CreateRules(ruleId, 20, 10, 60);
            IAlgorithm algorithm = new InProcessTokenBucketAlgorithm(rule, stubTimeProvider, updatable: true);

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

                if (i < 21)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }

                if (i >= 21 && i <= 40)
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
        public void UpdateRules_ExpendInflowUnit_TriggerLimit()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var ruleId = "UpdateRules_ExpendInflowUnit_TriggerLimit";
            var rule = CreateRules(ruleId, 20, 10, 20);
            IAlgorithm algorithm = new InProcessTokenBucketAlgorithm(rule, stubTimeProvider, updatable: true);

            for (int i = 1; i <= 50; i++)
            {
                // expend inflow unit
                if (i == 21)
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

                if (i < 21)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }

                // trigger limit
                if (i >= 21 && i <= 40)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                stubTimeProvider.IncrementMilliseconds(1);
            }
        }

        [DataTestMethod]
        public void UpdateRules_NarrowInflowQunatity_TriggerLimit()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var ruleId = "UpdateRules_NarrowInflowQunatity_TriggerLimit";
            var rule = CreateRules(ruleId, 10, 10, 10);
            IAlgorithm algorithm = new InProcessTokenBucketAlgorithm(rule, stubTimeProvider, updatable: true);

            for (int i = 1; i <= 50; i++)
            {
                if (i == 11)
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

                if (i == 10)
                {
                    Assert.AreEqual(0, result.RuleCheckResults.First().Count);
                }

                if (i == 11)
                {
                    Assert.AreEqual(4, result.RuleCheckResults.First().Count);
                }

                if (i == 16)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i == 21)
                {
                    Assert.AreEqual(4, result.RuleCheckResults.First().Count);
                }

                if (i == 31)
                {
                    Assert.AreEqual(4, result.RuleCheckResults.First().Count);
                }

                stubTimeProvider.IncrementMilliseconds(1);
            }
        }

        [DataTestMethod]
        public void UpdateRules_ExpendInflowQunatity_LoseLimit()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var ruleId = "UpdateRules_ExpendInflowQunatity_LoseLimit";
            var rule = CreateRules(ruleId, 10, 5, 10);
            IAlgorithm algorithm = new InProcessTokenBucketAlgorithm(rule, stubTimeProvider, updatable: true);

            for (int i = 1; i <= 50; i++)
            {
                if (i == 31)
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

                //Console.WriteLine($"{i},{result.RuleCheckResults.First().Count},{result.IsLimit}");

                if (i == 11)
                {
                    Assert.AreEqual(4, result.RuleCheckResults.First().Count);
                }

                if (i == 16)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i == 21)
                {
                    Assert.AreEqual(4, result.RuleCheckResults.First().Count);
                }

                if (i == 26)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                // lose limting
                if (i >= 31)
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
        public async Task LockAsync_LockThreeSeconds_Common()
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
        public async Task UpdateRulesAsync_RaiseCapacity_KeepLimit()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var ruleId = "UpdateRulesAsync_RaiseCapacity_KeepLimit";
            var rule = CreateRules(ruleId, 50, 10, 100);
            IAlgorithm algorithm = new InProcessTokenBucketAlgorithm(rule, stubTimeProvider, updatable: true);

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

                if (i < 51)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }

                if (i >= 51)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                stubTimeProvider.Increment();
            }
        }

        [DataTestMethod]
        public async Task UpdateRulesAsync_ReduceCapacity_TriggerLimit()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var ruleId = "UpdateRules_ReduceCapacity_TriggerLimit";
            var rule = CreateRules(ruleId, 100, 10, 100);
            IAlgorithm algorithm = new InProcessTokenBucketAlgorithm(rule, stubTimeProvider, updatable: true);

            for (int i = 1; i <= 80; i++)
            {
                if (i == 21)
                {
                    var newRule = CreateRules(ruleId, 30, 10, 100);
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

                if (i < 51)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }

                if (i >= 51)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                stubTimeProvider.Increment();
            }
        }

        [DataTestMethod]
        public async Task UpdateRulesAsync_NarrowInflowUnit_LoseLimit()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var ruleId = "UpdateRulesAsync_NarrowInflowUnit_LoseLimit";
            var rule = CreateRules(ruleId, 20, 10, 60);
            IAlgorithm algorithm = new InProcessTokenBucketAlgorithm(rule, stubTimeProvider, updatable: true);

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

                if (i < 21)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }

                if (i >= 21 && i <= 40)
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
        public async Task UpdateRulesAsync_ExpendInflowUnit_TriggerLimit()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var ruleId = "UpdateRulesAsync_ExpendInflowUnit_TriggerLimit";
            var rule = CreateRules(ruleId, 20, 10, 20);
            IAlgorithm algorithm = new InProcessTokenBucketAlgorithm(rule, stubTimeProvider, updatable: true);

            for (int i = 1; i <= 50; i++)
            {
                // expend inflow unit
                if (i == 21)
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

                if (i < 21)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }

                // trigger limit
                if (i >= 21 && i <= 40)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                stubTimeProvider.IncrementMilliseconds(1);
            }
        }

        [DataTestMethod]
        public async Task UpdateRulesAsync_NarrowInflowQunatity_TriggerLimit()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var ruleId = "UpdateRulesAsync_NarrowInflowQunatity_TriggerLimit";
            var rule = CreateRules(ruleId, 10, 10, 10);
            IAlgorithm algorithm = new InProcessTokenBucketAlgorithm(rule, stubTimeProvider, updatable: true);

            for (int i = 1; i <= 50; i++)
            {
                if (i == 11)
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

                if (i == 10)
                {
                    Assert.AreEqual(0, result.RuleCheckResults.First().Count);
                }

                if (i == 11)
                {
                    Assert.AreEqual(4, result.RuleCheckResults.First().Count);
                }

                if (i == 16)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i == 21)
                {
                    Assert.AreEqual(4, result.RuleCheckResults.First().Count);
                }

                if (i == 31)
                {
                    Assert.AreEqual(4, result.RuleCheckResults.First().Count);
                }

                stubTimeProvider.IncrementMilliseconds(1);
            }
        }

        [DataTestMethod]
        public async Task UpdateRulesAsync_ExpendInflowQunatity_LoseLimit()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var ruleId = "UpdateRulesAsync_ExpendInflowQunatity_LoseLimit";
            var rule = CreateRules(ruleId, 10, 5, 10);
            IAlgorithm algorithm = new InProcessTokenBucketAlgorithm(rule, stubTimeProvider, updatable: true);

            for (int i = 1; i <= 50; i++)
            {
                if (i == 31)
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

                //Console.WriteLine($"{i},{result.RuleCheckResults.First().Count},{result.IsLimit}");

                if (i == 11)
                {
                    Assert.AreEqual(4, result.RuleCheckResults.First().Count);
                }

                if (i == 16)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i == 21)
                {
                    Assert.AreEqual(4, result.RuleCheckResults.First().Count);
                }

                if (i == 26)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                // lose limting
                if (i >= 31)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }

                stubTimeProvider.IncrementMilliseconds(1);
            }
        }

        private static TokenBucketRule[] CreateRules(string ruleId, int capacity, int inflowQuantity, int inflowUnitMilliseconds)
        {
            return new TokenBucketRule[]
                {
                    new TokenBucketRule(capacity,inflowQuantity,TimeSpan.FromMilliseconds(inflowUnitMilliseconds))
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
