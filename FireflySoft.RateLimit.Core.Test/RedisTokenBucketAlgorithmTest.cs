using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using FireflySoft.RateLimit.Core.Rule;
using FireflySoft.RateLimit.Core.InProcessAlgorithm;
using FireflySoft.RateLimit.Core.RedisAlgorithm;
using System.Linq;

namespace FireflySoft.RateLimit.Core.Test
{
    [TestClass]
    public class RedisTokenBucketAlgorithmTest
    {
        [DataTestMethod]
        public void Common()
        {
            var processor = GetAlgorithm(20, 10, TimeSpan.FromSeconds(1), 0);

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

                if (i == 21 || i == 32 || i >= 43)
                {
                    Assert.AreEqual(true, checkResult.IsLimit);
                }
                else
                {
                    Assert.AreEqual(false, checkResult.IsLimit);
                }

                if (i == 21 || i == 32)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        [DataTestMethod]
        public void ResetTime_NotExistKey_ReturnTimeWindowAsExpireTime()
        {
            var outflowUnit = TimeSpan.FromSeconds(1);
            var processor = GetAlgorithm(10, 5, outflowUnit, 0);
            var expected = DateTimeOffset.Now.Add(outflowUnit);
            var result = processor.Check(new SimulationRequest()
            {
                RequestId = Guid.NewGuid().ToString(),
                RequestResource = "home",
                Parameters = new Dictionary<string, string>() { { "from", "sample" } }
            });

            Assert.IsTrue(expected.AddMilliseconds(10) >= result.RuleCheckResults.First().ResetTime);
            Assert.IsTrue(expected.AddMilliseconds(-10) <= result.RuleCheckResults.First().ResetTime);
        }

        [DataTestMethod]
        public void ResetTime_ExistKey_ReturnFirstPeriodExpireTime()
        {
            var outflowUnit = TimeSpan.FromSeconds(1);
            var processor = GetAlgorithm(10, 5, outflowUnit, 0);
            var expected = DateTimeOffset.Now.Add(outflowUnit);
            for (int i = 0; i < 3; i++)
            {
                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });
                //Console.WriteLine($"{expected.ToString("HH:mm:ss.fff")},{result.RuleCheckResults.First().ResetTime.ToString("HH:mm:ss.fff")}");
                Assert.IsTrue(expected.AddMilliseconds(10) >= result.RuleCheckResults.First().ResetTime);
                Assert.IsTrue(expected.AddMilliseconds(-10) <= result.RuleCheckResults.First().ResetTime);

                Thread.Sleep(10);
            }
        }

        [DataTestMethod]
        public void ResetTime_ExistKey_ReturnNextPeriodExpireTime()
        {
            var outflowUnit = TimeSpan.FromSeconds(1);
            var processor = GetAlgorithm(10, 5, outflowUnit, 0);
            DateTimeOffset expected = DateTimeOffset.MinValue;

            for (int i = 0; i < 3; i++)
            {
                if (i == 0)
                {
                    expected = DateTimeOffset.Now.Add(outflowUnit);
                }

                if (i == 2)
                {
                    expected = expected.Add(outflowUnit);
                }

                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i == 2)
                {
                    Console.WriteLine($"{expected.ToString("HH:mm:ss.fff")},{result.RuleCheckResults.First().ResetTime.ToString("HH:mm:ss.fff")}");
                    Assert.IsTrue(expected.AddMilliseconds(10) >= result.RuleCheckResults.First().ResetTime);
                    Assert.IsTrue(expected.AddMilliseconds(-10) <= result.RuleCheckResults.First().ResetTime);
                }

                Thread.Sleep(500);
            }
        }

        [DataTestMethod]
        public void ResetTime_TriggerLimit_ReturnLockExpireTime()
        {
            var outflowUnit = TimeSpan.FromMilliseconds(1000);
            var processor = GetAlgorithm(10, 5, outflowUnit, 3);
            DateTimeOffset expected = DateTimeOffset.MinValue;

            for (int i = 0; i < 20; i++)
            {
                if (i == 10)
                {
                    expected = DateTimeOffset.Now.Add(TimeSpan.FromSeconds(3));
                }

                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i >= 10)
                {
                    Assert.IsTrue(expected.AddMilliseconds(10) >= result.RuleCheckResults.First().ResetTime);
                    Assert.IsTrue(expected.AddMilliseconds(-10) <= result.RuleCheckResults.First().ResetTime);
                }
            }
        }

        [DataTestMethod]
        public void Lock_LockThreeSeconds_Common()
        {
            var processor = GetAlgorithm(30, 10, TimeSpan.FromSeconds(1), 3);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 61 && i == 62 && i == 63)
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
                    Thread.Sleep(200);
                }

                if (i == 42)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        [DataTestMethod]
        public void StartTimeType_FromCurrent_Common()
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
                    Thread.Sleep(200);
                }

                if (i == 42)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        [DataTestMethod]
        public void StartTime_Expire_NotExist()
        {
            var processor = GetAlgorithm(30, 10, TimeSpan.FromSeconds(1), 0, id: "1");

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

                if (i == 31 || i == 62)
                {
                    Assert.AreEqual(true, checkResult.IsLimit);
                }
                else
                {
                    Assert.AreEqual(false, checkResult.IsLimit);
                }

                if (i == 31 || i == 62)
                {
                    SpinWait.SpinUntil(() => { return false; }, 3000);

                    var redisClient = RedisClientHelper.GetClient();
                    bool exsit = redisClient.GetDatabase().KeyExists("1-home-st");
                    Assert.AreEqual(false, exsit);
                }
            }
        }

        [DataTestMethod]
        public async Task CommonAsync()
        {
            var processor = GetAlgorithm(20, 10, TimeSpan.FromSeconds(1), 0);

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

                if (i == 21 || i == 32 || i >= 43)
                {
                    Assert.AreEqual(true, checkResult.IsLimit);
                }
                else
                {
                    Assert.AreEqual(false, checkResult.IsLimit);
                }

                if (i == 21 || i == 32)
                {
                    await Task.Delay(1000);
                }
            }
        }

        [DataTestMethod]
        public async Task ResetTimeAsync_NotExistKey_ReturnTimeWindowAsExpireTime()
        {
            var outflowUnit = TimeSpan.FromSeconds(1);
            var processor = GetAlgorithm(10, 5, outflowUnit, 0);
            var expected = DateTimeOffset.Now.Add(outflowUnit);
            var result = await processor.CheckAsync(new SimulationRequest()
            {
                RequestId = Guid.NewGuid().ToString(),
                RequestResource = "home",
                Parameters = new Dictionary<string, string>() { { "from", "sample" } }
            });

            Assert.IsTrue(expected.AddMilliseconds(10) >= result.RuleCheckResults.First().ResetTime);
            Assert.IsTrue(expected.AddMilliseconds(-10) <= result.RuleCheckResults.First().ResetTime);
        }

        [DataTestMethod]
        public async Task ResetTimeAsync_ExistKey_ReturnFirstPeriodExpireTime()
        {
            var outflowUnit = TimeSpan.FromSeconds(1);
            var processor = GetAlgorithm(10, 5, outflowUnit, 0);
            var expected = DateTimeOffset.Now.Add(outflowUnit);
            for (int i = 0; i < 3; i++)
            {
                var result = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });
                Console.WriteLine($"{expected.ToString("HH:mm:ss.fff")},{result.RuleCheckResults.First().ResetTime.ToString("HH:mm:ss.fff")}");
                Assert.IsTrue(expected.AddMilliseconds(10) >= result.RuleCheckResults.First().ResetTime);
                Assert.IsTrue(expected.AddMilliseconds(-10) <= result.RuleCheckResults.First().ResetTime);

                Thread.Sleep(10);
            }
        }

        [DataTestMethod]
        public async Task ResetTimeAsync_ExistKey_ReturnNextPeriodExpireTime()
        {
            var outflowUnit = TimeSpan.FromSeconds(1);
            var processor = GetAlgorithm(10, 5, outflowUnit, 0);
            DateTimeOffset expected = DateTimeOffset.MinValue;

            for (int i = 0; i < 3; i++)
            {
                if (i == 0)
                {
                    expected = DateTimeOffset.Now.Add(outflowUnit);
                }

                if (i == 2)
                {
                    expected = expected.Add(outflowUnit);
                }

                var result = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i == 2)
                {
                    //Console.WriteLine($"{expected.ToString("HH:mm:ss.fff")},{result.RuleCheckResults.First().ResetTime.ToString("HH:mm:ss.fff")}");
                    Assert.IsTrue(expected.AddMilliseconds(10) >= result.RuleCheckResults.First().ResetTime);
                    Assert.IsTrue(expected.AddMilliseconds(-10) <= result.RuleCheckResults.First().ResetTime);
                }

                Thread.Sleep(500);
            }
        }

        [DataTestMethod]
        public async Task ResetTimeAsync_TriggerLimit_ReturnLockExpireTime()
        {
            var outflowUnit = TimeSpan.FromMilliseconds(1000);
            var processor = GetAlgorithm(10, 5, outflowUnit, 3);
            DateTimeOffset expected = DateTimeOffset.MinValue;

            for (int i = 0; i < 20; i++)
            {
                if (i == 10)
                {
                    expected = DateTimeOffset.Now.Add(TimeSpan.FromSeconds(3));
                }

                var result = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i >= 10)
                {
                    Assert.IsTrue(expected.AddMilliseconds(10) >= result.RuleCheckResults.First().ResetTime);
                    Assert.IsTrue(expected.AddMilliseconds(-10) <= result.RuleCheckResults.First().ResetTime);
                }
            }
        }

        [DataTestMethod]
        public async Task LockAsync_LockThreeSeconds_Common()
        {
            var processor = GetAlgorithm(30, 10, TimeSpan.FromSeconds(1), 3);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 61 && i == 62 && i == 63)
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
                    await Task.Delay(200);
                }

                if (i == 42)
                {
                    await Task.Delay(1000);
                }
            }
        }

        [DataTestMethod]
        public async Task StartTimeTypeAsync_FromCurrent_Common()
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
                    await Task.Delay(200);
                }

                if (i == 42)
                {
                    await Task.Delay(1000);
                }
            }
        }

        [DataTestMethod]
        public async Task UpdateRulesAsync_RaiseCapacity_KeepLimit()
        {
            var ruleId = "UpdateRulesAsync_RaiseCapacity_KeepLimit";
            var rule = CreateRules(ruleId, 50, 10, 200);
            IAlgorithm algorithm = new InProcessTokenBucketAlgorithm(rule, updatable: true);

            for (int i = 1; i <= 80; i++)
            {
                if (i == 71)
                {
                    var newRule = CreateRules(ruleId, 100, 10, 200);
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
            }
        }

        [DataTestMethod]
        public async Task UpdateRulesAsync_ReduceCapacity_TriggerLimit()
        {
            var ruleId = "UpdateRules_ReduceCapacity_TriggerLimit";
            var rule = CreateRules(ruleId, 100, 10, 200);
            IAlgorithm algorithm = new InProcessTokenBucketAlgorithm(rule, updatable: true);

            for (int i = 1; i <= 80; i++)
            {
                if (i == 21)
                {
                    var newRule = CreateRules(ruleId, 30, 10, 200);
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
            }
        }

        [DataTestMethod]
        public async Task UpdateRulesAsync_NarrowInflowUnit_LoseLimit()
        {
            var ruleId = "UpdateRulesAsync_NarrowInflowUnit_LoseLimit";
            var rule = CreateRules(ruleId, 10, 10, 1000);
            IAlgorithm algorithm = new InProcessTokenBucketAlgorithm(rule, updatable: true);

            for (int i = 1; i <= 30; i++)
            {
                if (i == 21)
                {
                    var newRule = CreateRules(ruleId, 10, 10, 600);
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

                if (i < 11)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }

                if (i >= 11 && i <= 20)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }
                if (i == 20)
                {
                    SpinWait.SpinUntil(() => { return false; }, 600);
                }

                // lose limit
                if (i >= 21)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public async Task UpdateRulesAsync_ExpendInflowUnit_TriggerLimit()
        {
            var ruleId = "UpdateRulesAsync_ExpendInflowUnit_TriggerLimit";
            var rule = CreateRules(ruleId, 20, 10, 200);
            IAlgorithm algorithm = new InProcessTokenBucketAlgorithm(rule, updatable: true);

            for (int i = 1; i <= 50; i++)
            {
                // expend inflow unit
                if (i == 21)
                {
                    var newRule = CreateRules(ruleId, 20, 10, 400);
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
                if (i == 20)
                {
                    SpinWait.SpinUntil(() => { return false; }, 200);
                }

                // trigger limit
                if (i >= 21 && i <= 40)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }
                if (i == 40)
                {
                    SpinWait.SpinUntil(() => { return false; }, 200);
                }
            }
        }

        [DataTestMethod]
        public async Task UpdateRulesAsync_NarrowInflowQunatity_TriggerLimit()
        {
            var redisClient = RedisClientHelper.GetClient();
            var ruleId = "UpdateRulesAsync_NarrowInflowQunatity_TriggerLimit";
            var rule = CreateRules(ruleId, 10, 10, 300);
            IAlgorithm algorithm = new RedisTokenBucketAlgorithm(rule, redisClient, updatable: true);

            for (int i = 1; i <= 50; i++)
            {
                if (i == 11)
                {
                    var newRule = CreateRules(ruleId, 10, 5, 300);
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

                //Console.WriteLine($"{i},{result.RuleCheckResults.First().Remaining},{result.IsLimit}");

                if (i == 10)
                {
                    Assert.AreEqual(0, result.RuleCheckResults.First().Remaining);
                }

                if (i == 10)
                {
                    SpinWait.SpinUntil(() => { return false; }, 300);
                }
                if (i == 11)
                {
                    Assert.AreEqual(4, result.RuleCheckResults.First().Remaining);
                }

                // trigger limit
                if (i == 16)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i == 20)
                {
                    SpinWait.SpinUntil(() => { return false; }, 300);
                }
                if (i == 21)
                {
                    Assert.AreEqual(4, result.RuleCheckResults.First().Remaining);
                }

                if (i == 30)
                {
                    SpinWait.SpinUntil(() => { return false; }, 300);
                }
                if (i == 31)
                {
                    Assert.AreEqual(4, result.RuleCheckResults.First().Remaining);
                }
            }
        }

        [DataTestMethod]
        public async Task UpdateRulesAsync_ExpendInflowQunatity_LoseLimit()
        {
            var redisClient = RedisClientHelper.GetClient();
            var ruleId = "UpdateRulesAsync_ExpendInflowQunatity_LoseLimit";
            var rule = CreateRules(ruleId, 10, 5, 300);
            IAlgorithm algorithm = new RedisTokenBucketAlgorithm(rule, redisClient, updatable: true);

            for (int i = 1; i <= 50; i++)
            {
                if (i == 31)
                {
                    var newRule = CreateRules(ruleId, 10, 10, 300);
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

                //Console.WriteLine($"{i},{result.RuleCheckResults.First().Remaining},{result.IsLimit}");

                if (i == 10)
                {
                    SpinWait.SpinUntil(() => { return false; }, 300);
                }
                if (i == 11)
                {
                    Assert.AreEqual(4, result.RuleCheckResults.First().Remaining);
                }

                if (i == 16)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i == 20)
                {
                    SpinWait.SpinUntil(() => { return false; }, 300);
                }
                if (i == 21)
                {
                    Assert.AreEqual(4, result.RuleCheckResults.First().Remaining);
                }

                if (i == 26)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                // lose limting
                if (i == 30)
                {
                    SpinWait.SpinUntil(() => { return false; }, 300);
                }
                if (i >= 31 && i <= 40)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
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

        private IAlgorithm GetAlgorithm(int capacity, int inflowQuantity, TimeSpan inflowUnit, int lockSeconds, StartTimeType startTimeType = StartTimeType.FromCurrent, string id = "")
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

            var redisClient = RedisClientHelper.GetClient();
            return new RedisTokenBucketAlgorithm(tokenBucketRules, redisClient);
        }
    }
}
