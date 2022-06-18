using System;
using System.Collections.Generic;
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
    public class RedisFixedWindowAlgorithmTest
    {
        [DataTestMethod]
        public void Common()
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
        public void ResetTime_NotExistKey_ReturnTimeWindowAsExpireTime()
        {
            var statWondow = TimeSpan.FromSeconds(1);
            var processor = GetAlgorithm(statWondow, StartTimeType.FromCurrent, 50, 0);

            var result = processor.Check(new SimulationRequest()
            {
                RequestId = Guid.NewGuid().ToString(),
                RequestResource = "home",
                Parameters = new Dictionary<string, string>() { { "from", "sample" } }
            });

            var expected = DateTimeOffset.Now.Add(statWondow);
            Assert.IsTrue(expected.AddMilliseconds(10) >= result.RuleCheckResults.First().ResetTime);
            Assert.IsTrue(expected.AddMilliseconds(-10) <= result.RuleCheckResults.First().ResetTime);
        }

        [DataTestMethod]
        public void ResetTime_ExistKey_ReturnFirstWindowExpireTime()
        {
            var statWondow = TimeSpan.FromSeconds(1);
            var processor = GetAlgorithm(statWondow, StartTimeType.FromCurrent, 50, 0);
            DateTimeOffset expected = DateTimeOffset.MinValue;
            for (int i = 0; i < 3; i++)
            {
                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i == 0)
                {
                    expected = DateTimeOffset.Now.Add(statWondow);
                }

                Assert.AreNotEqual(expected, DateTimeOffset.MinValue);
                Assert.IsTrue(expected.AddMilliseconds(10) >= result.RuleCheckResults.First().ResetTime);
                Assert.IsTrue(expected.AddMilliseconds(-10) <= result.RuleCheckResults.First().ResetTime);
            }
        }

        [DataTestMethod]
        public void ResetTime_ExistKey_ReturnNextWindowExpireTime()
        {
            var statWindow = TimeSpan.FromSeconds(1);
            var processor = GetAlgorithm(statWindow, StartTimeType.FromCurrent, 50, 0);
            DateTimeOffset expected = DateTimeOffset.MinValue;

            for (int i = 0; i < 4; i++)
            {
                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i == 3)
                {
                    expected = DateTimeOffset.Now.Add(statWindow);
                    Assert.AreNotEqual(expected, DateTimeOffset.MinValue);
                    Assert.IsTrue(expected.AddMilliseconds(10) >= result.RuleCheckResults.First().ResetTime);
                    Assert.IsTrue(expected.AddMilliseconds(-10) <= result.RuleCheckResults.First().ResetTime);
                }

                Thread.Sleep(400);
            }
        }

        [DataTestMethod]
        public void ResetTime_TriggerLimit_ReturnLockExpireTime()
        {
            var statWindow = TimeSpan.FromSeconds(1);
            var lockSeconds = 3;
            var processor = GetAlgorithm(statWindow, StartTimeType.FromCurrent, 20, lockSeconds);
            DateTimeOffset expected = DateTimeOffset.MinValue;

            for (int i = 0; i < 30; i++)
            {
                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i == 21)
                {
                    expected = DateTimeOffset.Now.AddSeconds(lockSeconds);
                }

                if (i >= 21)
                {
                    Assert.AreNotEqual(expected, DateTimeOffset.MinValue);
                    Assert.IsTrue(expected.AddMilliseconds(10) >= result.RuleCheckResults.First().ResetTime);
                    Assert.IsTrue(expected.AddMilliseconds(-10) <= result.RuleCheckResults.First().ResetTime);
                }
            }
        }

        [DataTestMethod]
        public void StartTimeType_FromNaturalPeriodBeign_Common()
        {
            var processor = GetAlgorithm(TimeSpan.FromSeconds(1), StartTimeType.FromNaturalPeriodBeign, 10, 0);

            while (true)
            {
                if (DateTimeOffset.Now.Millisecond < 500)
                {
                    SpinWait.SpinUntil(() => { return false; }, 10);
                    continue;
                }
                break;
            }

            for (int i = 1; i <= 20; i++)
            {
                if (i == 16)
                {
                    Thread.Sleep(500);
                }

                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i > 10 && i <= 15)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i <= 10 || i > 15)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public void Peek_NotExistKey_Common()
        {
            var statWindow = TimeSpan.FromSeconds(1);
            var processor = GetAlgorithm(statWindow, StartTimeType.FromCurrent, 10, 0);

            var result = processor.Peek("home");
            var firstResult = result.RuleCheckResults.First();

            Assert.AreEqual(DateTimeOffset.MinValue, firstResult.ResetTime);
            Assert.AreEqual(0, firstResult.Count);
            Assert.AreEqual(false, firstResult.IsLimit);
        }

        [DataTestMethod]
        public void Peek_ExistKey_Common()
        {
            var statWindow = TimeSpan.FromSeconds(1);
            var lockSeconds = 1;
            var processor = GetAlgorithm(statWindow, StartTimeType.FromCurrent, 10, lockSeconds);
            DateTimeOffset expected = DateTimeOffset.MinValue;

            for (int i = 1; i < 20; i++)
            {
                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i == 1)
                {
                    expected = DateTimeOffset.Now.Add(statWindow);
                }
                if (i == 11)
                {
                    expected = DateTimeOffset.Now.AddSeconds(lockSeconds);
                }

                if (i <= 10)
                {
                    var peekResult = processor.Peek("home");
                    var firstPeekResult = peekResult.RuleCheckResults.First();
                    Assert.IsTrue(expected.AddMilliseconds(10) >= firstPeekResult.ResetTime);
                    Assert.IsTrue(expected.AddMilliseconds(-10) <= firstPeekResult.ResetTime);
                    Assert.AreEqual(i, firstPeekResult.Count);
                    Assert.AreEqual(false, firstPeekResult.IsLimit);
                }

                if (i >= 11 && i <= 15)
                {
                    var peekResult = processor.Peek("home");
                    var firstPeekResult = peekResult.RuleCheckResults.First();
                    Assert.IsTrue(expected.AddMilliseconds(10) >= firstPeekResult.ResetTime);
                    Assert.IsTrue(expected.AddMilliseconds(-10) <= firstPeekResult.ResetTime);
                    Assert.AreEqual(-1, firstPeekResult.Count);
                    Assert.AreEqual(true, firstPeekResult.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public void StartTimeType_FromCurrent_Common()
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
        public void Lock_LockThreeSeconds_Common()
        {
            var processor = GetAlgorithm(TimeSpan.FromSeconds(1), StartTimeType.FromCurrent, 10, 3);

            for (int i = 1; i <= 30; i++)
            {
                if (i == 21 || i == 22 || i == 23)
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

                //Console.WriteLine(i + ":" + result.IsLimit);

                if (i > 10 && i <= 22)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i <= 10 || i > 22)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public void UpdateRules_RaiseLimitNumber_NoLimit()
        {
            var ruleId = "UpdateRules_RaiseLimitNumber_NoLimit";
            FixedWindowRule[] fixedWindowRules = CreateRules(50, ruleId);

            IAlgorithm algorithm;

            var redisClient = RedisClientHelper.GetClient();
            algorithm = new RedisFixedWindowAlgorithm(fixedWindowRules, redisClient, updatable: true);


            for (int i = 1; i <= 80; i++)
            {
                if (i == 61)
                {
                    var fixedWindowRules2 = CreateRules(60, ruleId);
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
            var ruleId = "UpdateRules_ReduceLimitNumber_NoLimit";
            FixedWindowRule[] fixedWindowRules = CreateRules(50, ruleId);

            IAlgorithm algorithm;

            var redisClient = RedisClientHelper.GetClient();
            algorithm = new RedisFixedWindowAlgorithm(fixedWindowRules, redisClient, updatable: true);


            for (int i = 1; i <= 80; i++)
            {
                if (i == 41)
                {
                    var fixedWindowRules2 = CreateRules(40, ruleId);
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
        public void UpdateRules_NarrowTimeWindowExpired_CreateNewTimeWindow()
        {
            var ruleId = "UpdateRules_NarrowTimeWindowExpired_CreateNewTimeWindow";
            FixedWindowRule[] fixedWindowRules = CreateRules(50, ruleId, 1000);

            IAlgorithm algorithm;
            var redisClient = RedisClientHelper.GetClient();
            algorithm = new RedisFixedWindowAlgorithm(fixedWindowRules, redisClient, updatable: true);


            for (int i = 1; i <= 70; i++)
            {
                if (i == 52)
                {
                    var fixedWindowRules2 = CreateRules(50, ruleId, 500);
                    algorithm.UpdateRules(fixedWindowRules2);
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

                if (i < 51 || i > 51)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public void UpdateRules_NarrowTimeWindowNotExpired_ResizeTimeWindow()
        {
            var ruleId = "UpdateRules_NarrowTimeWindowNotExpired_ResizeTimeWindow";
            FixedWindowRule[] fixedWindowRules = CreateRules(50, ruleId, 1000);

            IAlgorithm algorithm;
            var redisClient = RedisClientHelper.GetClient();
            algorithm = new RedisFixedWindowAlgorithm(fixedWindowRules, redisClient, updatable: true);


            for (int i = 1; i <= 70; i++)
            {
                if (i == 52)
                {
                    var fixedWindowRules2 = CreateRules(50, ruleId, 500);
                    algorithm.UpdateRules(fixedWindowRules2);
                }

                if (i == 61)
                {
                    Thread.Sleep(200);
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
                    Thread.Sleep(300);
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
            var ruleId = "UpdateRules_ExpendTimeWindowNotExpired_ResizeTimeWindow";
            FixedWindowRule[] fixedWindowRules = CreateRules(50, ruleId, 1000);

            IAlgorithm algorithm;
            var redisClient = RedisClientHelper.GetClient();
            algorithm = new RedisFixedWindowAlgorithm(fixedWindowRules, redisClient, updatable: true);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 52)
                {
                    var fixedWindowRules2 = CreateRules(50, ruleId, 2000);
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
        public async Task CommonAsync()
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
        public async Task ResetTimeAsync_NotExistKey_ReturnTimeWindowAsExpireTime()
        {
            var statWindow = TimeSpan.FromSeconds(1);
            var processor = GetAlgorithm(statWindow, StartTimeType.FromCurrent, 50, 0);
            var result = await processor.CheckAsync(new SimulationRequest()
            {
                RequestId = Guid.NewGuid().ToString(),
                RequestResource = "home",
                Parameters = new Dictionary<string, string>() { { "from", "sample" } }
            });

            var expected = DateTimeOffset.Now.Add(statWindow);

            Assert.IsTrue(expected.AddMilliseconds(10) >= result.RuleCheckResults.First().ResetTime);
            Assert.IsTrue(expected.AddMilliseconds(-10) <= result.RuleCheckResults.First().ResetTime);
        }

        [DataTestMethod]
        public async Task ResetTimeAsync_ExistKey_ReturnFirstWindowExpireTime()
        {
            var statWindow = TimeSpan.FromSeconds(1);
            var processor = GetAlgorithm(statWindow, StartTimeType.FromCurrent, 50, 0);
            DateTimeOffset expected = DateTimeOffset.MinValue;
            for (int i = 0; i < 5; i++)
            {
                var result = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i == 0)
                {
                    var now = DateTimeOffset.Now;
                    expected = now.Add(statWindow);
                    //Console.WriteLine($"-{statWindow.TotalMilliseconds},{now.ToString("ss.fff")},{expected.ToString("ss.fff")}");
                }
                Assert.AreNotEqual(expected, DateTimeOffset.MinValue);
                Assert.IsTrue(expected.AddMilliseconds(10) >= result.RuleCheckResults.First().ResetTime);
                Assert.IsTrue(expected.AddMilliseconds(-10) <= result.RuleCheckResults.First().ResetTime);
            }
        }

        [DataTestMethod]
        public async Task ResetTimeAsync_ExistKey_ReturnNextWindowExpireTime()
        {
            var statWindow = TimeSpan.FromSeconds(1);
            var processor = GetAlgorithm(statWindow, StartTimeType.FromCurrent, 50, 0);
            DateTimeOffset expected = DateTimeOffset.MinValue;

            for (int i = 0; i < 4; i++)
            {
                var result = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i == 3)
                {
                    expected = DateTimeOffset.Now.Add(statWindow);
                    Assert.AreNotEqual(expected, DateTimeOffset.MinValue);
                    Assert.IsTrue(expected.AddMilliseconds(10) >= result.RuleCheckResults.First().ResetTime);
                    Assert.IsTrue(expected.AddMilliseconds(-10) <= result.RuleCheckResults.First().ResetTime);
                }

                Thread.Sleep(400);
            }
        }

        [DataTestMethod]
        public async Task ResetTimeAsync_TriggerLimit_ReturnLockExpireTime()
        {
            var statWindow = TimeSpan.FromSeconds(1);
            var lockSeconds = 3;
            var processor = GetAlgorithm(statWindow, StartTimeType.FromCurrent, 20, lockSeconds);
            DateTimeOffset expected = DateTimeOffset.MinValue;

            for (int i = 0; i < 30; i++)
            {
                var result = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i == 21)
                {
                    expected = DateTimeOffset.Now.AddSeconds(lockSeconds);
                }

                if (i >= 21)
                {
                    Assert.AreNotEqual(expected, DateTimeOffset.MinValue);
                    Assert.IsTrue(expected.AddMilliseconds(10) >= result.RuleCheckResults.First().ResetTime);
                    Assert.IsTrue(expected.AddMilliseconds(-10) <= result.RuleCheckResults.First().ResetTime);
                }
            }
        }

        [DataTestMethod]
        public async Task StartTimeTypeAsync_FromNaturalPeriodBeign_Common()
        {
            var processor = GetAlgorithm(TimeSpan.FromSeconds(1), StartTimeType.FromNaturalPeriodBeign, 10, 0);

            while (true)
            {
                if (DateTimeOffset.Now.Millisecond < 500)
                {
                    Thread.Sleep(20);
                    continue;
                }
                break;
            }

            for (int i = 1; i <= 20; i++)
            {
                if (i == 16)
                {
                    Thread.Sleep(500);
                }
                var result = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i > 10 && i <= 15)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i <= 10 || i > 15)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public async Task StartTimeTypeAsync_FromCurrent_Common()
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
        public async Task LockAsync_LockThreeSeconds_Common()
        {
            var processor = GetAlgorithm(TimeSpan.FromSeconds(1), StartTimeType.FromCurrent, 20, 3);

            for (int i = 1; i <= 40; i++)
            {
                if (i == 31 || i == 32 || i == 33)
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

                if (i > 20 && i <= 32)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i <= 20 || i > 32)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public async Task UpdateRulesAsync_RaiseLimitNumber_NoLimit()
        {
            var ruleId = "UpdateRulesAsync_RaiseLimitNumber_NoLimit";
            FixedWindowRule[] fixedWindowRules = CreateRules(50, ruleId);

            IAlgorithm algorithm;

            var redisClient = RedisClientHelper.GetClient();
            algorithm = new RedisFixedWindowAlgorithm(fixedWindowRules, redisClient, updatable: true);


            for (int i = 1; i <= 80; i++)
            {
                if (i == 61)
                {
                    var fixedWindowRules2 = CreateRules(60, ruleId);
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
            var ruleId = "UpdateRulesAsync_ReduceLimitNumber_NoLimit";
            FixedWindowRule[] fixedWindowRules = CreateRules(50, ruleId);

            IAlgorithm algorithm;

            var redisClient = RedisClientHelper.GetClient();
            algorithm = new RedisFixedWindowAlgorithm(fixedWindowRules, redisClient, updatable: true);

            for (int i = 1; i <= 80; i++)
            {
                if (i == 41)
                {
                    var fixedWindowRules2 = CreateRules(40, ruleId);
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
        public async Task UpdateRulesAsync_NarrowTimeWindowExpired_CreateNewTimeWindow()
        {
            var ruleId = "UpdateRulesAsync_NarrowTimeWindowExpired_CreateNewTimeWindow";
            FixedWindowRule[] fixedWindowRules = CreateRules(50, ruleId, 1000);

            var redisClient = RedisClientHelper.GetClient();
            IAlgorithm algorithm = new RedisFixedWindowAlgorithm(fixedWindowRules, redisClient, updatable: true);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 52)
                {
                    var fixedWindowRules2 = CreateRules(50, ruleId, 500);
                    algorithm.UpdateRules(fixedWindowRules2);
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

                if (i < 51 || i > 51)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public async Task UpdateRulesAsync_NarrowTimeWindowNotExpired_ResizeTimeWindow()
        {
            var ruleId = "UpdateRulesAsync_NarrowTimeWindowNotExpired_ResizeTimeWindow";
            FixedWindowRule[] fixedWindowRules = CreateRules(50, ruleId, 1000);

            IAlgorithm algorithm;
            var redisClient = RedisClientHelper.GetClient();
            algorithm = new RedisFixedWindowAlgorithm(fixedWindowRules, redisClient, updatable: true);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 52)
                {
                    var fixedWindowRules2 = CreateRules(50, ruleId, 500);
                    algorithm.UpdateRules(fixedWindowRules2);
                }

                if (i == 61)
                {
                    Thread.Sleep(200);
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
                    Thread.Sleep(300);
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
            var ruleId = "UpdateRulesAsync_ExpendTimeWindowNotExpired_ResizeTimeWindow";
            FixedWindowRule[] fixedWindowRules = CreateRules(50, ruleId, 1000);

            var redisClient = RedisClientHelper.GetClient();
            IAlgorithm algorithm = new RedisFixedWindowAlgorithm(fixedWindowRules, redisClient, updatable: true);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 52)
                {
                    var fixedWindowRules2 = CreateRules(50, ruleId, 2000);
                    algorithm.UpdateRules(fixedWindowRules2);
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
                //Console.WriteLine(DateTime.Now.Second + ":" + i + ":" + result.RuleCheckResults.First().Count);

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

        private static FixedWindowRule[] CreateRules(int limitNumber, string ruleId, long statWindowMilliseconds = 1000)
        {
            return new FixedWindowRule[]
                {
                    new FixedWindowRule()
                    {
                        Id=ruleId,
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