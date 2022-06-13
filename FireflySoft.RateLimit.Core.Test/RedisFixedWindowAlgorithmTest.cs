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
        public void Test()
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
        public void StartTimeType_FromNaturalPeriodBeign_Common()
        {
            var processor = GetAlgorithm(TimeSpan.FromSeconds(1), StartTimeType.FromNaturalPeriodBeign, 50, 0);

            while (true)
            {
                if (DateTimeOffset.Now.Millisecond < 700)
                {
                    SpinWait.SpinUntil(() => { return false; }, 10);
                    continue;
                }
                break;
            }

            for (int i = 1; i <= 70; i++)
            {
                if (i == 56)
                {
                    Thread.Sleep(300);
                }

                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i > 50 && i <= 55)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i <= 50 || i > 55)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public void TestFromCurrent()
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
        public async Task TestAsync()
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
        public async Task TestFromNaturalPeriodBeignAsync()
        {
            var processor = GetAlgorithm(TimeSpan.FromSeconds(1), StartTimeType.FromNaturalPeriodBeign, 50, 0);

            while (true)
            {
                if (DateTimeOffset.Now.Millisecond < 800)
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
                    Thread.Sleep(200);
                }
                var result = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() {
                                { "from","sample" },
                        }
                });

                if (i > 50 && i <= 55)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i <= 50 || i > 55)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public async Task TestFromCurrentAsync()
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
        public void TestLockSeconds()
        {
            var processor = GetAlgorithm(TimeSpan.FromSeconds(1), StartTimeType.FromCurrent, 20, 3);

            for (int i = 1; i <= 40; i++)
            {
                 if (i == 31 || i == 32 || i == 33)
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
        public async Task TestLockSecondsAsync()
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