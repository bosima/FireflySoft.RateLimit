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
            Thread.Sleep(6000);
        }

        [DataTestMethod]
        [DataRow("memory")]
        [DataRow("redis")]
        public void TestUpdateRules(string storageType)
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

                Console.WriteLine(i + ":" + result.IsLimit + "," + result.RuleCheckResults.First().Count);

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
        public async Task TestUpdateRulesAsync(string storageType)
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

                Console.WriteLine(i + ":" + result.IsLimit + "," + result.RuleCheckResults.First().Count);

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

        private static FixedWindowRule[] CreateRules(int limitNumber)
        {
            return new FixedWindowRule[]
                {
                    new FixedWindowRule()
                    {
                        Id="TestUpdateRules-1",
                        StatWindow=TimeSpan.FromSeconds(1),
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