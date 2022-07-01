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
    public class RedisLeakyBucketAlgorithmTest
    {
        [DataTestMethod]
        public void Common()
        {
            var processor = GetAlgorithm(20, 10, TimeSpan.FromSeconds(1), 0);

            for (int i = 1; i <= 50; i++)
            {
                var checkResult = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i == 31 || i >= 42)
                {
                    Assert.AreEqual(true, checkResult.IsLimit);
                }
                else
                {
                    Assert.AreEqual(false, checkResult.IsLimit);
                }

                if (i == 31)
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
                Console.WriteLine($"{expected.ToString("HH:mm:ss.fff")},{result.RuleCheckResults.First().ResetTime.ToString("HH:mm:ss.fff")}");
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
                if (i == 16)
                {
                    expected = DateTimeOffset.Now.Add(TimeSpan.FromSeconds(3));
                }

                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i >= 16)
                {
                    Assert.IsTrue(expected.AddMilliseconds(10) >= result.RuleCheckResults.First().ResetTime);
                    Assert.IsTrue(expected.AddMilliseconds(-10) <= result.RuleCheckResults.First().ResetTime);
                }
            }
        }

        [DataTestMethod]
        public void ResetTime_TriggerLimitNoLock_ReturnPeriodExpireTime()
        {
            var outflowUnit = TimeSpan.FromMilliseconds(1000);
            var processor = GetAlgorithm(10, 5, outflowUnit, 0);
            DateTimeOffset expected = DateTimeOffset.Now.Add(outflowUnit);

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
                    Assert.IsTrue(expected.AddMilliseconds(10) >= result.RuleCheckResults.First().ResetTime);
                    Assert.IsTrue(expected.AddMilliseconds(-10) <= result.RuleCheckResults.First().ResetTime);
                }
            }
        }

        [DataTestMethod]
        public void Wait_Common()
        {
            var processor = GetAlgorithm(30, 10, TimeSpan.FromSeconds(1), 0);

            for (int i = 1; i <= 50; i++)
            {
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

                var waitOn11 = 0L;
                if (i == 11)
                {
                    waitOn11 = checkResult.RuleCheckResults.First().Wait;
                    Assert.AreEqual(true, waitOn11 > 0);
                }

                var waitOn12 = 0L;
                if (i == 12)
                {
                    waitOn12 = checkResult.RuleCheckResults.First().Wait;
                    Assert.AreEqual(true, waitOn12 > 0);
                    Assert.AreEqual(true, waitOn12 >= waitOn11);
                }
                var waitOn21 = 0L;
                if (i == 21)
                {
                    waitOn21 = checkResult.RuleCheckResults.First().Wait;
                    Assert.AreEqual(true, waitOn21 > 1000);
                }
                var waitOn22 = 0L;
                if (i == 22)
                {
                    waitOn22 = checkResult.RuleCheckResults.First().Wait;
                    Assert.AreEqual(true, waitOn22 > 0);
                    Assert.AreEqual(true, waitOn22 >= waitOn21);
                }
            }
        }

        [DataTestMethod]
        public void Wait_AcrossStatWindow_ContinueWait()
        {
            // capacity: 20
            // outflow_unit: 500ms
            // outflow_quantity_per_unit: 5
            // amount: 1
            var processor = GetAlgorithm(20, 5, TimeSpan.FromMilliseconds(500), 0);

            for (int i = 1; i <= 30; i++)
            {
                // take to the second period after 25th request
                if (i == 26)
                {
                    Thread.Sleep(500);
                }

                var checkResult = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                // the last request of the first period
                if (i == 25)
                {
                    Assert.AreEqual(25, checkResult.RuleCheckResults.First().Count);
                    Assert.IsTrue(checkResult.RuleCheckResults.First().Wait > 1800);
                }

                // the first request of the second period
                if (i == 26)
                {
                    // first, five requests flow out of the leaky bucket, 
                    // then, one request is added to the leaky bucket.
                    // the current number of requests in the leaky bucket is: 16 = 20 - 5 + 1, 
                    // and this request will be processed after the last 15th requests,
                    // so the current request must wait, and all requests in the second period must wait.
                    Assert.AreEqual(21, checkResult.RuleCheckResults.First().Count);
                    Assert.IsTrue(checkResult.RuleCheckResults.First().Wait > 1800);
                }
            }
        }

        [DataTestMethod]
        public void Lock_LockThreeSeconds_Common()
        {
            var processor = GetAlgorithm(30, 10, TimeSpan.FromSeconds(1), 3);

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
            var processor = GetAlgorithm(10, 10, TimeSpan.FromSeconds(1), 0, StartTimeType.FromNaturalPeriodBeign);

            while (true)
            {
               if (DateTimeOffset.Now.Millisecond < 400 ||  DateTimeOffset.Now.Millisecond > 500)
                {
                    SpinWait.SpinUntil(() => false, 10);
                    continue;
                }
                break;
            }

            for (int i = 1; i <= 50; i++)
            {
                var checkResult = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                //Console.WriteLine($"{DateTimeOffset.Now.ToString("mm:ss.fff")},{i},{checkResult.RuleCheckResults.First().Count}");

                if (i == 21 || i == 32 || i >= 43)
                {
                    Assert.AreEqual(true, checkResult.IsLimit);
                }
                else
                {
                    Assert.AreEqual(false, checkResult.IsLimit);
                }

                if (i == 21)
                {
                    Thread.Sleep(600);
                }

                if (i == 32)
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
                    Thread.Sleep(200);
                }

                if (i == 52)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        [DataTestMethod]
        public void StartTimeExpire_ExpireAfter3Seconds_NotExsit()
        {
            var ruleId = "1";
            var processor = GetAlgorithm(30, 10, TimeSpan.FromSeconds(1), 0, id: ruleId);

            for (int i = 1; i <= 90; i++)
            {
                var checkResult = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i == 41 || i >= 82)
                {
                    Assert.AreEqual(true, checkResult.IsLimit);
                }
                else
                {
                    Assert.AreEqual(false, checkResult.IsLimit);
                }

                if (i == 41)
                {
                    SpinWait.SpinUntil(() => { return false; }, 4000);

                    var redisClient = RedisClientHelper.GetClient();
                    bool exsit = redisClient.GetDatabase().KeyExists($"{ruleId}-home-st");
                    Assert.AreEqual(false, exsit);
                }
            }
        }

        [DataTestMethod]
        public void UpdateRules_RaiseCapacity_LoseLimit()
        {
            var redisClient = RedisClientHelper.GetClient();
            var ruleId = "UpdateRules_RaiseCapacity_LoseLimit";
            var rule = CreateRules(ruleId, 50, 10, 1000);
            IAlgorithm algorithm = new RedisLeakyBucketAlgorithm(rule, redisClient, updatable: true);

            for (int i = 1; i <= 80; i++)
            {
                if (i == 71)
                {
                    var newRule = CreateRules(ruleId, 100, 10, 1000);
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
            var redisClient = RedisClientHelper.GetClient();
            var ruleId = "UpdateRules_ReduceCapacity_TriggerLimit";
            var rule = CreateRules(ruleId, 100, 10, 1000);
            IAlgorithm algorithm = new RedisLeakyBucketAlgorithm(rule, redisClient, updatable: true);

            for (int i = 1; i <= 80; i++)
            {
                if (i == 51)
                {
                    var newRule = CreateRules(ruleId, 40, 10, 1000);
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
            var redisClient = RedisClientHelper.GetClient();
            var ruleId = "UpdateRules_NarrowOutflowUnit_LoseLimit";
            var rule = CreateRules(ruleId, 10, 10, 800);
            IAlgorithm algorithm = new RedisLeakyBucketAlgorithm(rule, redisClient, updatable: true);

            for (int i = 1; i <= 40; i++)
            {
                if (i == 31)
                {
                    var newRule = CreateRules(ruleId, 10, 10, 400);
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

                if (i >= 21 && i <= 30)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                // lose limit
                if (i >= 31)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }

                SpinWait.SpinUntil(() => { return false; }, 10);
            }
        }

        [DataTestMethod]
        public void UpdateRules_ExpendOutflowUnit_TriggerLimit()
        {
            var redisClient = RedisClientHelper.GetClient();
            var ruleId = "UpdateRules_ExpendOutflowUnit_TriggerLimit";
            var rule = CreateRules(ruleId, 20, 10, 600);
            IAlgorithm algorithm = new RedisLeakyBucketAlgorithm(rule, redisClient, updatable: true);

            for (int i = 1; i <= 50; i++)
            {
                // expend outflow unit
                if (i == 31)
                {
                    var newRule = CreateRules(ruleId, 20, 10, 1200);
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

                //Console.WriteLine($"{DateTime.Now.ToString("mm:ss.fff")},{i},{result.RuleCheckResults.First().Count}");

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

                SpinWait.SpinUntil(() => { return false; }, 10);
            }
        }

        [DataTestMethod]
        public void UpdateRules_NarrowOutflowQunatity_TriggerLimit()
        {
            var redisClient = RedisClientHelper.GetClient();
            var ruleId = "UpdateRules_NarrowOutflowQunatity_TriggerLimit";
            var rule = CreateRules(ruleId, 10, 10, 300);
            IAlgorithm algorithm = new RedisLeakyBucketAlgorithm(rule, redisClient, updatable: true);

            for (int i = 1; i <= 50; i++)
            {
                if (i == 21)
                {
                    var newRule = CreateRules(ruleId, 10, 5, 300);
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

                //Console.WriteLine($"{DateTime.Now.ToString("mm:ss.fff")},{i},{result.RuleCheckResults.First().Count},{result.IsLimit}");

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

                if (i == 10 || i == 20)
                {
                    Thread.Sleep(300);
                }
            }
        }

        [DataTestMethod]
        public void UpdateRules_ExpendOutflowQunatity_LoseLimit()
        {
            var redisClient = RedisClientHelper.GetClient();
            var ruleId = "UpdateRules_ExpendOutflowQunatity_LoseLimit";
            var rule = CreateRules(ruleId, 10, 5, 300);
            IAlgorithm algorithm = new RedisLeakyBucketAlgorithm(rule, redisClient, updatable: true);

            for (int i = 1; i <= 50; i++)
            {
                if (i == 27)
                {
                    var newRule = CreateRules(ruleId, 10, 10, 300);
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

                if (i == 10 || i == 20 || i == 30 || i == 40)
                {
                    Thread.Sleep(300);
                }
            }
        }

        [DataTestMethod]
        public async Task CommonAsync()
        {
            var processor = GetAlgorithm(20, 10, TimeSpan.FromSeconds(1), 1);

            for (int i = 1; i <= 50; i++)
            {
                var checkResult = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i == 31 || i >= 42)
                {
                    Assert.AreEqual(true, checkResult.IsLimit);
                }
                else
                {
                    Assert.AreEqual(false, checkResult.IsLimit);
                }

                if (i == 31)
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
            var outflowUnit = TimeSpan.FromMilliseconds(1000);
            var processor = GetAlgorithm(10, 5, outflowUnit, 3);
            DateTimeOffset expected = DateTimeOffset.MinValue;

            for (int i = 0; i < 20; i++)
            {
                if (i >= 16)
                {
                    expected = DateTimeOffset.Now.Add(TimeSpan.FromSeconds(3));
                }

                var result = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i >= 16)
                {
                    Assert.IsTrue(expected.AddMilliseconds(10) >= result.RuleCheckResults.First().ResetTime);
                    Assert.IsTrue(expected.AddMilliseconds(-10) <= result.RuleCheckResults.First().ResetTime);
                }
            }
        }

        [DataTestMethod]
        public async Task ResetTimeAsync_TriggerLimitNoLock_ReturnPeriodExpireTime()
        {
            var outflowUnit = TimeSpan.FromMilliseconds(1000);
            var processor = GetAlgorithm(10, 5, outflowUnit, 0);
            DateTimeOffset expected = DateTimeOffset.Now.Add(outflowUnit);

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
                    Assert.IsTrue(expected.AddMilliseconds(10) >= result.RuleCheckResults.First().ResetTime);
                    Assert.IsTrue(expected.AddMilliseconds(-10) <= result.RuleCheckResults.First().ResetTime);
                }
            }
        }

        [DataTestMethod]
        public async Task WaitAsync_CommonAsync()
        {
            var processor = GetAlgorithm(30, 10, TimeSpan.FromSeconds(1), 0);

            for (int i = 1; i <= 50; i++)
            {
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

                var waitOn11 = 0L;
                if (i == 11)
                {
                    waitOn11 = checkResult.RuleCheckResults.First().Wait;
                    Assert.AreEqual(true, waitOn11 > 0);
                }

                var waitOn12 = 0L;
                if (i == 12)
                {
                    waitOn12 = checkResult.RuleCheckResults.First().Wait;
                    Assert.AreEqual(true, waitOn12 > 0);
                    Assert.AreEqual(true, waitOn12 >= waitOn11);
                }
                var waitOn21 = 0L;
                if (i == 21)
                {
                    waitOn21 = checkResult.RuleCheckResults.First().Wait;
                    Assert.AreEqual(true, waitOn21 > 1000);
                }
                var waitOn22 = 0L;
                if (i == 22)
                {
                    waitOn22 = checkResult.RuleCheckResults.First().Wait;
                    Assert.AreEqual(true, waitOn22 > 0);
                    Assert.AreEqual(true, waitOn22 >= waitOn21);
                }
            }
        }

        [DataTestMethod]
        public async Task WaitAsync_AcrossStatWindow_ContinueWait()
        {
            // capacity: 20
            // outflow_unit: 500ms
            // outflow_quantity_per_unit: 5
            // amount: 1
            var processor = GetAlgorithm(20, 5, TimeSpan.FromMilliseconds(500), 0);

            for (int i = 1; i <= 30; i++)
            {
                // take to the second period after 25th request
                if (i == 26)
                {
                    Thread.Sleep(500);
                }

                var checkResult = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                // the last request of the first period
                if (i == 25)
                {
                    Assert.AreEqual(25, checkResult.RuleCheckResults.First().Count);
                    Assert.IsTrue(checkResult.RuleCheckResults.First().Wait > 1800);
                }

                // the first request of the second period
                if (i == 26)
                {
                    // first, five requests flow out of the leaky bucket, 
                    // then, one request is added to the leaky bucket.
                    // the current number of requests in the leaky bucket is: 16 = 20 - 5 + 1, 
                    // and this request will be processed after the last 15th requests,
                    // so the current request must wait, and all requests in the second period must wait.
                    Assert.AreEqual(21, checkResult.RuleCheckResults.First().Count);
                    Assert.IsTrue(checkResult.RuleCheckResults.First().Wait > 1800);
                }
            }
        }

        [DataTestMethod]
        public async Task LockAsync_LockThreeSeconds_Common()
        {
            var processor = GetAlgorithm(30, 10, TimeSpan.FromSeconds(1), 3);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 61 || i == 62 || i == 63)
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
            var processor = GetAlgorithm(10, 10, TimeSpan.FromSeconds(1), 0, StartTimeType.FromNaturalPeriodBeign);

            while (true)
            {
                if (DateTimeOffset.Now.Millisecond < 400 ||  DateTimeOffset.Now.Millisecond > 500)
                {
                    SpinWait.SpinUntil(() => false, 10);
                    continue;
                }
                break;
            }

            for (int i = 1; i <= 50; i++)
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

                if (i == 21)
                {
                    Thread.Sleep(600);
                }

                if (i == 32)
                {
                    Thread.Sleep(1000);
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
                    Thread.Sleep(200);
                }

                if (i == 52)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        [DataTestMethod]
        public async Task StartTimeExpireAsync_ExpireAfter3Seconds_NotExsit()
        {
            var ruleId = "1";
            var processor = GetAlgorithm(30, 10, TimeSpan.FromSeconds(1), 0, id: ruleId);

            for (int i = 1; i <= 90; i++)
            {
                var checkResult = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i == 41 || i >= 82)
                {
                    Assert.AreEqual(true, checkResult.IsLimit);
                }
                else
                {
                    Assert.AreEqual(false, checkResult.IsLimit);
                }

                if (i == 41)
                {
                    SpinWait.SpinUntil(() => { return false; }, 4000);

                    var redisClient = RedisClientHelper.GetClient();
                    bool exsit = redisClient.GetDatabase().KeyExists($"{ruleId}-home-st");
                    Assert.AreEqual(false, exsit);
                }
            }
        }

        [DataTestMethod]
        public async Task UpdateRulesAsync_RaiseCapacity_LoseLimit()
        {
            var redisClient = RedisClientHelper.GetClient();
            var ruleId = "UpdateRulesAsync_RaiseCapacity_LoseLimit";
            var rule = CreateRules(ruleId, 50, 10, 1000);
            IAlgorithm algorithm = new RedisLeakyBucketAlgorithm(rule, redisClient, updatable: true);

            for (int i = 1; i <= 80; i++)
            {
                if (i == 71)
                {
                    var newRule = CreateRules(ruleId, 100, 10, 1000);
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
            var redisClient = RedisClientHelper.GetClient();
            var ruleId = "UpdateRulesAsync_ReduceCapacity_TriggerLimit";
            var rule = CreateRules(ruleId, 100, 10, 1000);
            IAlgorithm algorithm = new RedisLeakyBucketAlgorithm(rule, redisClient, updatable: true);

            for (int i = 1; i <= 80; i++)
            {
                if (i == 51)
                {
                    var newRule = CreateRules(ruleId, 40, 10, 1000);
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
            var redisClient = RedisClientHelper.GetClient();
            var ruleId = "UpdateRulesAsync_NarrowOutflowUnit_LoseLimit";
            var rule = CreateRules(ruleId, 10, 10, 600);
            IAlgorithm algorithm = new RedisLeakyBucketAlgorithm(rule, redisClient, updatable: true);

            for (int i = 1; i <= 40; i++)
            {
                if (i == 31)
                {
                    var newRule = CreateRules(ruleId, 10, 10, 400);
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

                if (i >= 21 && i <= 30)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                // lose limit
                if (i >= 31)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }

                SpinWait.SpinUntil(() => { return false; }, 10);
            }
        }

        [DataTestMethod]
        public async Task UpdateRulesAsync_ExpendOutflowUnit_TriggerLimit()
        {
            var redisClient = RedisClientHelper.GetClient();
            var ruleId = "UpdateRulesAsync_ExpendOutflowUnit_TriggerLimit";
            var rule = CreateRules(ruleId, 20, 10, 600);
            IAlgorithm algorithm = new RedisLeakyBucketAlgorithm(rule, redisClient, updatable: true);

            for (int i = 1; i <= 50; i++)
            {
                // expend outflow unit
                if (i == 31)
                {
                    var newRule = CreateRules(ruleId, 20, 10, 1200);
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

                //Console.WriteLine($"{DateTime.Now.ToString("mm:ss.fff")},{i},{result.RuleCheckResults.First().Count}");

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

                SpinWait.SpinUntil(() => { return false; }, 10);
            }
        }

        [DataTestMethod]
        public async Task UpdateRulesAsync_NarrowOutflowQunatity_TriggerLimit()
        {
            var redisClient = RedisClientHelper.GetClient();
            var ruleId = "UpdateRulesAsync_NarrowOutflowQunatity_TriggerLimit";
            var rule = CreateRules(ruleId, 10, 10, 300);
            IAlgorithm algorithm = new RedisLeakyBucketAlgorithm(rule, redisClient, updatable: true);

            for (int i = 1; i <= 50; i++)
            {
                if (i == 21)
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

                //Console.WriteLine($"{DateTime.Now.ToString("mm:ss.fff")},{i},{result.RuleCheckResults.First().Count},{result.IsLimit}");

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

                if (i == 10 || i == 20)
                {
                    Thread.Sleep(300);
                }
            }
        }

        [DataTestMethod]
        public async Task UpdateRulesAsync_ExpendOutflowQunatity_LoseLimit()
        {
            var redisClient = RedisClientHelper.GetClient();
            var ruleId = "UpdateRulesAsync_ExpendOutflowQunatity_LoseLimit";
            var rule = CreateRules(ruleId, 10, 5, 300);
            IAlgorithm algorithm = new RedisLeakyBucketAlgorithm(rule, redisClient, updatable: true);

            for (int i = 1; i <= 50; i++)
            {
                if (i == 27)
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

                if (i == 10 || i == 20 || i == 30 || i == 40)
                {
                    Thread.Sleep(300);
                }
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

        private IAlgorithm GetAlgorithm(int capacity, int outflowQuantity, TimeSpan outflowUnit, int lockSeconds, StartTimeType startTimeType = StartTimeType.FromCurrent, string id = "")
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

            var redisClient = RedisClientHelper.GetClient();
            return new RedisLeakyBucketAlgorithm(leakyBucketRules, redisClient);
        }
    }
}