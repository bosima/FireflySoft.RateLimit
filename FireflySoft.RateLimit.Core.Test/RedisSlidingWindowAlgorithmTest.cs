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
        public void Common()
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
        public void ResetTime_NotExistKey_ReturnTimeWindowAsExpireTime()
        {
            var statWindow = TimeSpan.FromSeconds(3);
            var statPeriod = TimeSpan.FromSeconds(1);
            var processor = GetAlgorithm(statWindow, statPeriod, StartTimeType.FromCurrent, 10, 0);

            var expected = DateTimeOffset.Now.Add(statPeriod);
            var result = processor.Check(new SimulationRequest()
            {
                RequestId = Guid.NewGuid().ToString(),
                RequestResource = "home",
                Parameters = new Dictionary<string, string>() { { "from", "sample" } }
            });

            //Console.WriteLine($"{expected.ToString("MM-dd HH:mm:ss.fff")},{result.RuleCheckResults.First().ResetTime.ToString("MM-dd HH:mm:ss.fff")}");
            Assert.IsTrue(expected.AddMilliseconds(10) >= result.RuleCheckResults.First().ResetTime);
            Assert.IsTrue(expected.AddMilliseconds(-10) <= result.RuleCheckResults.First().ResetTime);
        }

        [DataTestMethod]
        public void ResetTime_ExistKey_ReturnFirstPeriodExpireTime()
        {
            var statWindow = TimeSpan.FromSeconds(3);
            var statPeriod = TimeSpan.FromSeconds(1);
            var processor = GetAlgorithm(statWindow, statPeriod, StartTimeType.FromCurrent, 10, 0);

            var expected = DateTimeOffset.Now.Add(statPeriod);
            for (int i = 0; i < 3; i++)
            {
                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                Assert.IsTrue(expected.AddMilliseconds(10) >= result.RuleCheckResults.First().ResetTime);
                Assert.IsTrue(expected.AddMilliseconds(-10) <= result.RuleCheckResults.First().ResetTime);

                Thread.Sleep(10);
            }
        }

        [DataTestMethod]
        public void ResetTime_ExistKey_ReturnNextPeriodExpireTime()
        {
            var statWindow = TimeSpan.FromSeconds(3);
            var statPeriod = TimeSpan.FromSeconds(1);
            var processor = GetAlgorithm(statWindow, statPeriod, StartTimeType.FromCurrent, 10, 0);
            DateTimeOffset expected = DateTimeOffset.MinValue;

            for (int i = 0; i < 3; i++)
            {
                if (i == 0)
                {
                    expected = DateTimeOffset.Now.Add(statPeriod);
                }
                if (i == 2)
                {
                    expected = expected.Add(statPeriod);
                }

                var result = processor.Check(new SimulationRequest()
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
        public void ResetTime_TriggerLimit_ReturnLockExpireTime()
        {
            var statWindow = TimeSpan.FromMilliseconds(600);
            var statPeriod = TimeSpan.FromMilliseconds(200);
            var processor = GetAlgorithm(statWindow, statPeriod, StartTimeType.FromCurrent, 10, 3);
            DateTimeOffset expected = DateTimeOffset.MinValue;

            for (int i = 0; i < 10; i++)
            {
                if (i == 11)
                {
                    expected = DateTimeOffset.Now.Add(TimeSpan.FromSeconds(3));
                }

                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i >= 11)
                {
                    Assert.IsTrue(expected.AddMilliseconds(10) >= result.RuleCheckResults.First().ResetTime);
                    Assert.IsTrue(expected.AddMilliseconds(-10) <= result.RuleCheckResults.First().ResetTime);
                }
            }
        }

        [DataTestMethod]
        public void Lock_LockThreeSeconds_Common()
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
        public void StartTimeType_FromNaturalPeriodBeign_Common()
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
        public void StartTimeType_FromCurrent_Common()
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

        [DataTestMethod]
        public void UpdateRules_RaiseLimitNumber_LoseLimit()
        {
            var ruleId = "UpdateRules_RaiseLimitNumber_LoseLimit";

            SlidingWindowRule[] rule = CreateRules(50, ruleId, 1000, 100);

            var redisClient = RedisClientHelper.GetClient();
            IAlgorithm algorithm = new RedisSlidingWindowAlgorithm(rule, redisClient, updatable: true);

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

            var rule = CreateRules(50, ruleId, 1000, 100);
            var redisClient = RedisClientHelper.GetClient();
            IAlgorithm algorithm = new RedisSlidingWindowAlgorithm(rule, redisClient, updatable: true);

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

            var rule = CreateRules(50, ruleId, 1000, 100);
            var redisClient = RedisClientHelper.GetClient();
            IAlgorithm algorithm = new RedisSlidingWindowAlgorithm(rule, redisClient, updatable: true);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 52)
                {
                    var newRule = CreateRules(50, ruleId, 500, 100);
                    algorithm.UpdateRules(newRule);
                    Thread.Sleep(500);
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

            var rule = CreateRules(50, ruleId, 1000, 100);
            var redisClient = RedisClientHelper.GetClient();
            IAlgorithm algorithm = new RedisSlidingWindowAlgorithm(rule, redisClient, updatable: true);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 52)
                {
                    var newRule = CreateRules(50, ruleId, 500, 100);
                    algorithm.UpdateRules(newRule);
                    Thread.Sleep(100);
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

            var rule = CreateRules(40, ruleId, 6000, 1000);
            var redisClient = RedisClientHelper.GetClient();
            IAlgorithm algorithm = new RedisSlidingWindowAlgorithm(rule, redisClient, updatable: true);

            int[] increamentNumber = new int[] { 11, 21, 31, 41, 51, 61, 71 };

            for (int i = 1; i <= 80; i++)
            {
                if (increamentNumber.Contains(i))
                {
                    Thread.Sleep(1000);
                }

                if (i == 42)
                {
                    var newRule = CreateRules(40, ruleId, 3000, 1000);
                    algorithm.UpdateRules(newRule);
                    Thread.Sleep(100);
                }

                var result = algorithm.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i == 41)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i == 42)
                {
                    Assert.AreEqual(21, result.RuleCheckResults.First().Count);
                }

                if (i == 51)
                {
                    Assert.AreEqual(20, result.RuleCheckResults.First().Count);
                }

                if (i > 41)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public void UpdateRules_ExpendTimeWindowNotExpired_Keeplimit()
        {
            var ruleId = "UpdateRulesAsync_ExpendTimeWindowNotExpired_Keeplimit";

            var rule = CreateRules(50, ruleId, 1000, 100);
            var redisClient = RedisClientHelper.GetClient();
            IAlgorithm algorithm = new RedisSlidingWindowAlgorithm(rule, redisClient, updatable: true);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 52)
                {
                    var fixedWindowRules2 = CreateRules(50, ruleId, 2000, 100);
                    algorithm.UpdateRules(fixedWindowRules2);
                }

                if (i == 61)
                {
                    Thread.Sleep(1000);
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
                    Thread.Sleep(1000);
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

            var rule = CreateRules(50, ruleId, 1000, 100);
            var redisClient = RedisClientHelper.GetClient();
            IAlgorithm algorithm = new RedisSlidingWindowAlgorithm(rule, redisClient, updatable: true);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 52)
                {
                    var fixedWindowRules2 = CreateRules(50, ruleId, 2000, 100);
                    algorithm.UpdateRules(fixedWindowRules2);
                }

                if (i == 61)
                {
                    Thread.Sleep(1000);
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
                    Thread.Sleep(1000);
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
        public void UpdateRules_ExpendTimeWindowNotExpired_Nolimit()
        {
            var ruleId = "UpdateRules_ExpendTimeWindowNotExpired_Nolimit";

            var rule = CreateRules(90, ruleId, 1000, 100);
            var redisClient = RedisClientHelper.GetClient();
            IAlgorithm algorithm = new RedisSlidingWindowAlgorithm(rule, redisClient, updatable: true);

            int[] increamentNumber = new int[] { 11, 21, 31, 41, 51, 61, 71, 81, 91, 101, 111 };

            for (int i = 1; i <= 120; i++)
            {
                if (increamentNumber.Contains(i))
                {
                    Thread.Sleep(300);
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

            var rule = CreateRules(50, ruleId, 1000, 100);
            var redisClient = RedisClientHelper.GetClient();
            IAlgorithm algorithm = new RedisSlidingWindowAlgorithm(rule, redisClient, updatable: true);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 50)
                {
                    var newRule = CreateRules(50, ruleId, 1000, 40);
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

                if (i == 50)
                {
                    Assert.AreEqual(1, result.RuleCheckResults.First().Count);
                }

                Assert.AreEqual(false, result.IsLimit);
            }
        }

        [DataTestMethod]
        public async Task CommonAsync()
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
        public async Task ResetTimeAsync_NotExistKey_ReturnTimeWindowAsExpireTime()
        {
            var statWindow = TimeSpan.FromSeconds(3);
            var statPeriod = TimeSpan.FromSeconds(1);
            var processor = GetAlgorithm(statWindow, statPeriod, StartTimeType.FromCurrent, 10, 0);

            var expected = DateTimeOffset.Now.Add(statPeriod);
            var result = await processor.CheckAsync(new SimulationRequest()
            {
                RequestId = Guid.NewGuid().ToString(),
                RequestResource = "home",
                Parameters = new Dictionary<string, string>() { { "from", "sample" } }
            });

            //Console.WriteLine($"{expected.ToString("MM-dd HH:mm:ss.fff")},{result.RuleCheckResults.First().ResetTime.ToString("MM-dd HH:mm:ss.fff")}");
            Assert.IsTrue(expected.AddMilliseconds(10) >= result.RuleCheckResults.First().ResetTime);
            Assert.IsTrue(expected.AddMilliseconds(-10) <= result.RuleCheckResults.First().ResetTime);
        }

        [DataTestMethod]
        public async Task ResetTimeAsync_ExistKey_ReturnFirstPeriodExpireTime()
        {
            var statWindow = TimeSpan.FromSeconds(3);
            var statPeriod = TimeSpan.FromSeconds(1);
            var processor = GetAlgorithm(statWindow, statPeriod, StartTimeType.FromCurrent, 10, 0);

            var expected = DateTimeOffset.Now.Add(statPeriod);
            for (int i = 0; i < 3; i++)
            {
                var result = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                Assert.IsTrue(expected.AddMilliseconds(10) >= result.RuleCheckResults.First().ResetTime);
                Assert.IsTrue(expected.AddMilliseconds(-10) <= result.RuleCheckResults.First().ResetTime);

                await Task.Delay(10);
            }
        }

        [DataTestMethod]
        public async Task ResetTimeAsync_ExistKey_ReturnNextPeriodExpireTime()
        {
            var statWindow = TimeSpan.FromSeconds(3);
            var statPeriod = TimeSpan.FromSeconds(1);
            var processor = GetAlgorithm(statWindow, statPeriod, StartTimeType.FromCurrent, 10, 0);
            DateTimeOffset expected = DateTimeOffset.MinValue;

            for (int i = 0; i < 3; i++)
            {
                if (i == 0)
                {
                    expected = DateTimeOffset.Now.Add(statPeriod);
                }
                if (i == 2)
                {
                    expected = expected.Add(statPeriod);
                }

                var result = await processor.CheckAsync(new SimulationRequest()
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
        public async Task ResetTimeAsync_TriggerLimit_ReturnLockExpireTime()
        {
            var statWindow = TimeSpan.FromMilliseconds(600);
            var statPeriod = TimeSpan.FromMilliseconds(200);
            var processor = GetAlgorithm(statWindow, statPeriod, StartTimeType.FromCurrent, 10, 3);
            DateTimeOffset expected = DateTimeOffset.MinValue;

            for (int i = 0; i < 10; i++)
            {
                if (i == 11)
                {
                    expected = DateTimeOffset.Now.Add(TimeSpan.FromSeconds(3));
                }

                var result = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i >= 11)
                {
                    Assert.IsTrue(expected.AddMilliseconds(10) >= result.RuleCheckResults.First().ResetTime);
                    Assert.IsTrue(expected.AddMilliseconds(-10) <= result.RuleCheckResults.First().ResetTime);
                }
            }
        }

        [DataTestMethod]
        public async Task LockAsync_LockThreeSeconds_Common()
        {
            var processor = GetAlgorithm(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(100), StartTimeType.FromCurrent, 20, 3);

            for (int i = 1; i <= 40; i++)
            {
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
                    await Task.Delay(1000);
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
        public async Task StartTimeTypeAsync_FromCurrent_Common()
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
        public async Task UpdateRulesAsync_RaiseLimitNumber_LoseLimit()
        {
            var ruleId = "UpdateRulesAsync_RaiseLimitNumber_LoseLimit";

            var rule = CreateRules(50, ruleId, 1000, 100);
            var redisClient = RedisClientHelper.GetClient();
            IAlgorithm algorithm = new RedisSlidingWindowAlgorithm(rule, redisClient, updatable: true);

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

            var rule = CreateRules(50, ruleId, 1000, 100);
            var redisClient = RedisClientHelper.GetClient();
            IAlgorithm algorithm = new RedisSlidingWindowAlgorithm(rule, redisClient, updatable: true);

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

            var rule = CreateRules(50, ruleId, 1000, 100);
            var redisClient = RedisClientHelper.GetClient();
            IAlgorithm algorithm = new RedisSlidingWindowAlgorithm(rule, redisClient, updatable: true);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 52)
                {
                    var newRule = CreateRules(50, ruleId, 500, 100);
                    await algorithm.UpdateRulesAsync(newRule);
                    Thread.Sleep(500);
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

            var rule = CreateRules(50, ruleId, 1000, 100);
            var redisClient = RedisClientHelper.GetClient();
            IAlgorithm algorithm = new RedisSlidingWindowAlgorithm(rule, redisClient, updatable: true);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 52)
                {
                    var newRule = CreateRules(50, ruleId, 500, 100);
                    await algorithm.UpdateRulesAsync(newRule);
                    Thread.Sleep(100);
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

            var rule = CreateRules(40, ruleId, 6000, 1000);
            var redisClient = RedisClientHelper.GetClient();
            IAlgorithm algorithm = new RedisSlidingWindowAlgorithm(rule, redisClient, updatable: true);

            int[] increamentNumber = new int[] { 11, 21, 31, 41, 51, 61, 71 };

            for (int i = 1; i <= 80; i++)
            {
                if (increamentNumber.Contains(i))
                {
                    Thread.Sleep(1000);
                }

                if (i == 42)
                {
                    var newRule = CreateRules(40, ruleId, 3000, 1000);
                    await algorithm.UpdateRulesAsync(newRule);
                    Thread.Sleep(100);
                }

                var result = await algorithm.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i == 41)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i == 42)
                {
                    Assert.AreEqual(21, result.RuleCheckResults.First().Count);
                }

                if (i == 51)
                {
                    Assert.AreEqual(20, result.RuleCheckResults.First().Count);
                }

                if (i > 41)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public async Task UpdateRulesAsync_ExpendTimeWindowNotExpired_Keeplimit()
        {
            var ruleId = "UpdateRulesAsync_ExpendTimeWindowNotExpired_Keeplimit";

            var rule = CreateRules(50, ruleId, 1000, 100);
            var redisClient = RedisClientHelper.GetClient();
            IAlgorithm algorithm = new RedisSlidingWindowAlgorithm(rule, redisClient, updatable: true);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 52)
                {
                    var fixedWindowRules2 = CreateRules(50, ruleId, 2000, 100);
                    await algorithm.UpdateRulesAsync(fixedWindowRules2);
                }

                if (i == 61)
                {
                    Thread.Sleep(1000);
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
                    Thread.Sleep(1000);
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

            var rule = CreateRules(40, ruleId, 3000, 1000);
            var redisClient = RedisClientHelper.GetClient();
            IAlgorithm algorithm = new RedisSlidingWindowAlgorithm(rule, redisClient, updatable: true);

            int[] increamentNumber = new int[] { 11, 21, 31, 41, 51, 61, 71 };

            for (int i = 1; i <= 80; i++)
            {
                if (increamentNumber.Contains(i))
                {
                    Thread.Sleep(1000);
                }

                if (i == 52)
                {
                    var newRule = CreateRules(40, ruleId, 5000, 1000);
                    await algorithm.UpdateRulesAsync(newRule);
                    Thread.Sleep(100);
                }

                var result = await algorithm.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                // Console.WriteLine($"{DateTimeOffset.Now.ToString("mm.ss.fff")},{i},{result.RuleCheckResults.First().Count}");

                if (i == 51)
                {
                    Assert.AreEqual(21, result.RuleCheckResults.First().Count);
                }

                // The expiration time of the period is twice as long as that of the time window,
                // so the period counted in front can be read here
                if (i >= 52 && i < 61)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                // A Period Expired
                if (i == 61)
                {
                    Assert.AreEqual(32, result.RuleCheckResults.First().Count);
                }

                if (i >= 61 && i < 70)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }

                if (i == 70)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                // A Period Expired: 10+10+1+9=31
                if (i == 71)
                {
                    Assert.AreEqual(31, result.RuleCheckResults.First().Count);
                }

                if (i >= 71)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public async Task UpdateRulesAsync_ExpendTimeWindowNotExpired_Nolimit()
        {
            var ruleId = "UpdateRulesAsync_ExpendTimeWindowNotExpired_Nolimit";

            var rule = CreateRules(90, ruleId, 1000, 100);
            var redisClient = RedisClientHelper.GetClient();
            IAlgorithm algorithm = new RedisSlidingWindowAlgorithm(rule, redisClient, updatable: true);

            int[] increamentNumber = new int[] { 11, 21, 31, 41, 51, 61, 71, 81, 91, 101, 111 };

            for (int i = 1; i <= 120; i++)
            {
                if (increamentNumber.Contains(i))
                {
                    Thread.Sleep(300);
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

            var rule = CreateRules(50, ruleId, 1000, 100);
            var redisClient = RedisClientHelper.GetClient();
            IAlgorithm algorithm = new RedisSlidingWindowAlgorithm(rule, redisClient, updatable: true);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 50)
                {
                    var newRule = CreateRules(50, ruleId, 900, 30);
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

                //Console.WriteLine($"{DateTimeOffset.Now.ToString("mm:ss.fff")},{i},{result.RuleCheckResults.First().Count}");

                if (i == 50)
                {
                    Assert.AreEqual(1, result.RuleCheckResults.First().Count);
                }

                Assert.AreEqual(false, result.IsLimit);
            }
        }

        [DataTestMethod]
        public async Task UpdateRulesAsync_ChangeStatPeriod_InheritPeriods()
        {
            var ruleId = "UpdateRulesAsync_ChangeStatPeriod_InheritPeriods";

            var rule = CreateRules(50, ruleId, 1000, 200);
            var redisClient = RedisClientHelper.GetClient();
            IAlgorithm algorithm = new RedisSlidingWindowAlgorithm(rule, redisClient, updatable: true);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 11 || i == 21 || i == 31 || i == 41)
                {
                    Thread.Sleep(200);
                }

                if (i == 42)
                {
                    var newRule = CreateRules(50, ruleId, 1000, 100);
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

                //Console.WriteLine($"{DateTimeOffset.Now.ToString("mm:ss.fff")},{i},{result.RuleCheckResults.First().Count}");

                if (i == 50)
                {
                    Assert.AreEqual(50, result.RuleCheckResults.First().Count);
                }

                if (i > 50)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }
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