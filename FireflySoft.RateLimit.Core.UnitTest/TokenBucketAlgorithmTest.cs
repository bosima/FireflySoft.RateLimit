using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FireflySoft.RateLimit.Core.UnitTest
{
    [TestClass]
    public class TokenBucketAlgorithmTest
    {
        [ClassInitialize]
        public static void Init(TestContext tc)
        {
        }
        
        [DataTestMethod]
        [DataRow("memory")]
        [DataRow("redis")]
        public void TestTokenBucketAlgorithm(string storageType)
        {
            var processor = GetTokenBucketProcessor(storageType);

            for (int i = 1; i <= 80; i++)
            {
                Debug.WriteLine("for " + i);

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
                    Thread.Sleep(1000);
                }
            }
        }

        [DataTestMethod]
        [DataRow("memory")]
        [DataRow("redis")]
        public async Task TestTokenBucketAlgorithmAsync(string storageType)
        {
            var processor = GetTokenBucketProcessor(storageType);

            for (int i = 1; i <= 80; i++)
            {
                Debug.WriteLine("for " + i);

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
                    Thread.Sleep(1000);
                }
            }
        }

        private RateLimitProcessor<SimulationRequest> GetTokenBucketProcessor(string storageType)
        {
            var tokenBucketRules = new TokenBucketRateLimitRule<SimulationRequest>[]
                {
                    new TokenBucketRateLimitRule<SimulationRequest>(30,10,TimeSpan.FromSeconds(1))
                    {
                        Id=Guid.NewGuid().ToString(),
                        LockSeconds=1,
                        ExtractTarget = (request) =>
                        {
                            return request.RequestResource;
                        },
                        CheckRuleMatching = (request) =>
                        {
                            return true;
                        },
                    }
                };

            IRateLimitStorage storage = new InProcessMemoryStorage();
            if (storageType == "redis")
            {
                storage = new RedisStorage(StackExchange.Redis.ConnectionMultiplexer.Connect("127.0.0.1"));
            }

            return new RateLimitProcessor<SimulationRequest>.Builder()
                .WithAlgorithm(new TokenBucketAlgorithm<SimulationRequest>(tokenBucketRules))
                .WithStorage(storage)
                .WithError(new RateLimitError()
                {
                    Code = 429,
                })
                .Build();
        }
    }
}
