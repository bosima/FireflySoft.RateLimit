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
    public class BaseAlgorithmTest
    {
        [TestInitialize()]
        public void TestInitialize()
        {
            Thread.Sleep(1000);
        }

        [DataTestMethod]
        [DataRow("memory")]
        [DataRow("redis")]
        public void UpdateRules_RaiseLimitNumber_NoLimit(string storageType)
        {
            FixedWindowRule[] fixedWindowRules = CreateRules(50);

            IAlgorithm algorithm;
            if (storageType == "redis")
            {
                var redisClient = RedisClientHelper.GetClient();
                algorithm = new RedisFixedWindowAlgorithm(fixedWindowRules, redisClient, updatable: true);
            }
            else
            {
                algorithm = new InProcessFixedWindowAlgorithm(fixedWindowRules, updatable: true);
            }

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
        [DataRow("memory")]
        [DataRow("redis")]
        public void UpdateRules_ReduceLimitNumber_NoLimit(string storageType)
        {
            FixedWindowRule[] fixedWindowRules = CreateRules(50);

            IAlgorithm algorithm;
            if (storageType == "redis")
            {
                var redisClient = RedisClientHelper.GetClient();
                algorithm = new RedisFixedWindowAlgorithm(fixedWindowRules, redisClient, updatable: true);
            }
            else
            {
                algorithm = new InProcessFixedWindowAlgorithm(fixedWindowRules, updatable: true);
            }

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
        [DataRow("memory")]
        [DataRow("redis")]
        public async Task UpdateRulesAsync_RaiseLimitNumber_NoLimit(string storageType)
        {
            FixedWindowRule[] fixedWindowRules = CreateRules(50);

            IAlgorithm algorithm;
            if (storageType == "redis")
            {
                var redisClient = RedisClientHelper.GetClient();
                algorithm = new RedisFixedWindowAlgorithm(fixedWindowRules, redisClient, updatable: true);
            }
            else
            {
                algorithm = new InProcessFixedWindowAlgorithm(fixedWindowRules, updatable: true);
            }

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
        [DataRow("memory")]
        [DataRow("redis")]
        public async Task UpdateRulesAsync_ReduceLimitNumber_NoLimit(string storageType)
        {
            FixedWindowRule[] fixedWindowRules = CreateRules(50);

            IAlgorithm algorithm;
            if (storageType == "redis")
            {
                var redisClient = RedisClientHelper.GetClient();
                algorithm = new RedisFixedWindowAlgorithm(fixedWindowRules, redisClient, updatable: true);
            }
            else
            {
                algorithm = new InProcessFixedWindowAlgorithm(fixedWindowRules, updatable: true);
            }

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
        [DataRow("memory")]
        [DataRow("redis")]
        public void UpdateRules_NarrowTimeWindowExpired_CreateNewTimeWindow(string storageType)
        {
            FixedWindowRule[] fixedWindowRules = CreateRules(50, 1000);

            IAlgorithm algorithm;
            if (storageType == "redis")
            {
                var redisClient = RedisClientHelper.GetClient();
                algorithm = new RedisFixedWindowAlgorithm(fixedWindowRules, redisClient, updatable: true);
            }
            else
            {
                algorithm = new InProcessFixedWindowAlgorithm(fixedWindowRules, updatable: true);
            }

            for (int i = 1; i <= 70; i++)
            {
                if (i == 52)
                {
                    var fixedWindowRules2 = CreateRules(50, 500);
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
        [DataRow("memory")]
        [DataRow("redis")]
        public void UpdateRules_NarrowTimeWindowNotExpired_ResizeTimeWindow(string storageType)
        {
            FixedWindowRule[] fixedWindowRules = CreateRules(50, 1000);

            IAlgorithm algorithm;
            if (storageType == "redis")
            {
                var redisClient = RedisClientHelper.GetClient();
                algorithm = new RedisFixedWindowAlgorithm(fixedWindowRules, redisClient, updatable: true);
            }
            else
            {
                algorithm = new InProcessFixedWindowAlgorithm(fixedWindowRules, updatable: true);
            }

            for (int i = 1; i <= 70; i++)
            {
                if (i == 52)
                {
                    var fixedWindowRules2 = CreateRules(50, 500);
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
        [DataRow("memory")]
        [DataRow("redis")]
        public void UpdateRules_ExpendTimeWindowNotExpired_ResizeTimeWindow(string storageType)
        {
            FixedWindowRule[] fixedWindowRules = CreateRules(50, 1000);

            IAlgorithm algorithm;
            if (storageType == "redis")
            {
                var redisClient = RedisClientHelper.GetClient();
                algorithm = new RedisFixedWindowAlgorithm(fixedWindowRules, redisClient, updatable: true);
            }
            else
            {
                algorithm = new InProcessFixedWindowAlgorithm(fixedWindowRules, updatable: true);
            }

            for (int i = 1; i <= 70; i++)
            {
                if (i == 52)
                {
                    var fixedWindowRules2 = CreateRules(50, 2000);
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
        [DataRow("memory")]
        [DataRow("redis")]
        public async Task UpdateRulesAsync_NarrowTimeWindowExpired_CreateNewTimeWindow(string storageType)
        {
            FixedWindowRule[] fixedWindowRules = CreateRules(50, 1000);

            IAlgorithm algorithm;
            if (storageType == "redis")
            {
                var redisClient = RedisClientHelper.GetClient();
                algorithm = new RedisFixedWindowAlgorithm(fixedWindowRules, redisClient, updatable: true);
            }
            else
            {
                algorithm = new InProcessFixedWindowAlgorithm(fixedWindowRules, updatable: true);
            }

            for (int i = 1; i <= 70; i++)
            {
                if (i == 52)
                {
                    var fixedWindowRules2 = CreateRules(50, 500);
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
        [DataRow("memory")]
        [DataRow("redis")]
        public async Task UpdateRulesAsync_NarrowTimeWindowNotExpired_ResizeTimeWindow(string storageType)
        {
            FixedWindowRule[] fixedWindowRules = CreateRules(50, 1000);

            IAlgorithm algorithm;
            if (storageType == "redis")
            {
                var redisClient = RedisClientHelper.GetClient();
                algorithm = new RedisFixedWindowAlgorithm(fixedWindowRules, redisClient, updatable: true);
            }
            else
            {
                algorithm = new InProcessFixedWindowAlgorithm(fixedWindowRules, updatable: true);
            }

            for (int i = 1; i <= 70; i++)
            {
                if (i == 52)
                {
                    var fixedWindowRules2 = CreateRules(50, 500);
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
        [DataRow("memory")]
        [DataRow("redis")]
        public async Task UpdateRulesAsync_ExpendTimeWindowNotExpired_ResizeTimeWindow(string storageType)
        {
            FixedWindowRule[] fixedWindowRules = CreateRules(50, 1000);

            IAlgorithm algorithm;
            if (storageType == "redis")
            {
                var redisClient = RedisClientHelper.GetClient();
                algorithm = new RedisFixedWindowAlgorithm(fixedWindowRules, redisClient, updatable: true);
            }
            else
            {
                algorithm = new InProcessFixedWindowAlgorithm(fixedWindowRules, updatable: true);
            }

            for (int i = 1; i <= 70; i++)
            {
                if (i == 52)
                {
                    var fixedWindowRules2 = CreateRules(50, 2000);
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

                if (i == 52)
                {
                    Thread.Sleep(1000);
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
    }
}