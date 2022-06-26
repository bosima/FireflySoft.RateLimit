using System.Linq;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FireflySoft.RateLimit.Core.InProcessAlgorithm;
using FireflySoft.RateLimit.Core.Rule;
using FireflySoft.RateLimit.Core.Time;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FireflySoft.RateLimit.Core.Test
{
    [TestClass]
    public class InProcessFixedWindowAlgorithmTest
    {
        [DataTestMethod]
        public void Common()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, TimeSpan.FromMilliseconds(60), StartTimeType.FromCurrent, 50, 0);

            // 0-50 51-60 61-110 111-120 121-140
            for (int i = 1; i <= 140; i++)
            {
                stubTimeProvider.Increment();

                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if ((i >= 51 && i <= 60) || (i >= 111 && i <= 120))
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i <= 50 || (i >= 61 && i <= 110) || i >= 121)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public void ResetTime_NotExistKey_ReturnTimeWindowAsExpireTime()
        {
            var now = DateTimeOffset.Now;
            var statWondow = TimeSpan.FromMilliseconds(60);
            var stubTimeProvider = new TestTimeProvider(now, TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, statWondow, StartTimeType.FromCurrent, 50, 0);

            var result = processor.Check(new SimulationRequest()
            {
                RequestId = Guid.NewGuid().ToString(),
                RequestResource = "home",
                Parameters = new Dictionary<string, string>() { { "from", "sample" } }
            });

            var expected = now.Add(statWondow);
            Assert.AreEqual(expected, result.RuleCheckResults.First().ResetTime);
        }

        [DataTestMethod]
        public void ResetTime_ExistKey_ReturnFirstWindowExpireTime()
        {
            var now = DateTimeOffset.Now;
            var statWindow = TimeSpan.FromMilliseconds(60);
            var stubTimeProvider = new TestTimeProvider(now, TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, statWindow, StartTimeType.FromCurrent, 50, 0);

            for (int i = 0; i < 3; i++)
            {
                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                var expected = now.Add(statWindow);
                Assert.AreEqual(expected, result.RuleCheckResults.First().ResetTime);

                stubTimeProvider.Increment();
            }
        }

        [DataTestMethod]
        public void ResetTime_ExistKey_ReturnNextWindowExpireTime()
        {
            var now = DateTimeOffset.Now;
            var statWindow = TimeSpan.FromMilliseconds(60);
            var stubTimeProvider = new TestTimeProvider(now, TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, statWindow, StartTimeType.FromCurrent, 50, 0);

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
                    var expected = now.Add(statWindow);
                    Assert.AreEqual(expected, result.RuleCheckResults.First().ResetTime);
                }

                stubTimeProvider.IncrementMilliseconds(30);
            }
        }

        [DataTestMethod]
        public void ResetTime_TriggerLimit_ReturnLockExpireTime()
        {
            var now = DateTimeOffset.Now;
            var statWindow = TimeSpan.FromMilliseconds(60);
            var stubTimeProvider = new TestTimeProvider(now, TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, statWindow, StartTimeType.FromCurrent, 50, 3);

            for (int i = 0; i < 60; i++)
            {
                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i >= 51)
                {
                    var expected = now.Add(TimeSpan.FromSeconds(3));
                    Assert.AreEqual(expected, result.RuleCheckResults.First().ResetTime);
                }
            }
        }

        [DataTestMethod]
        public void ResetTime_TriggerLimitNoLock_ReturnPeriodExpireTime()
        {
            var statWindow = TimeSpan.FromSeconds(1);
            var now = DateTimeOffset.Now;
            var stubTimeProvider = new TestTimeProvider(now, TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, statWindow, StartTimeType.FromCurrent, 10, 0);
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
                    expected = stubTimeProvider.GetCurrentLocalTime().Add(statWindow);
                }

                if (i >= 11)
                {
                    Assert.AreNotEqual(expected, DateTimeOffset.MinValue);
                    Assert.AreEqual(expected, result.RuleCheckResults.First().ResetTime);
                    Assert.AreEqual(expected,result.RuleCheckResults.First().ResetTime);
                }
                stubTimeProvider.Increment();
            }
        }

        [DataTestMethod]
        public void Peek_NotExistKey_Common()
        {
            var now = DateTimeOffset.Now;
            var statWondow = TimeSpan.FromMilliseconds(60);
            var stubTimeProvider = new TestTimeProvider(now, TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, statWondow, StartTimeType.FromCurrent, 50, 0);

            var result = processor.Peek("home");
            var firstResult = result.RuleCheckResults.First();

            Assert.AreEqual(DateTimeOffset.MinValue, firstResult.ResetTime);
            Assert.AreEqual(0, firstResult.Count);
            Assert.AreEqual(false, firstResult.IsLimit);
        }

        [DataTestMethod]
        public void Peek_ExistKey_Common()
        {
            var statWindow = TimeSpan.FromMilliseconds(65);
            var now = DateTimeOffset.Now;
            var lockSeconds = 1;
            var stubTimeProvider = new TestTimeProvider(now, TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, statWindow, StartTimeType.FromCurrent, 60, lockSeconds);
            DateTimeOffset expected = DateTimeOffset.MinValue;

            for (int i = 1; i < 70; i++)
            {
                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i == 1)
                {
                    expected = now.Add(statWindow);
                }
                if (i == 61)
                {
                    expected = stubTimeProvider.GetCurrentLocalTime().AddSeconds(lockSeconds);
                }

                if (i <= 60)
                {
                    var peekResult = processor.Peek("home");
                    var firstPeekResult = peekResult.RuleCheckResults.First();
                    Assert.AreEqual(expected, firstPeekResult.ResetTime);
                    Assert.AreEqual(i, firstPeekResult.Count);
                    Assert.AreEqual(false, firstPeekResult.IsLimit);
                }

                if (i >= 61 && i <= 65)
                {
                    var peekResult = processor.Peek("home");
                    var firstPeekResult = peekResult.RuleCheckResults.First();
                    Assert.AreEqual(expected, firstPeekResult.ResetTime);
                    Assert.AreEqual(-1, firstPeekResult.Count);
                    Assert.AreEqual(true, firstPeekResult.IsLimit);
                }

                stubTimeProvider.Increment();
            }
        }

        [DataTestMethod]
        public void StartTimeType_FromNaturalPeriodBeign_Common()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, TimeSpan.FromSeconds(1), StartTimeType.FromNaturalPeriodBeign, 50, 0);

            stubTimeProvider.IncrementMilliseconds(800);

            // 1-50 51-55 56-70
            for (int i = 1; i <= 70; i++)
            {
                stubTimeProvider.Increment();

                if (i == 56)
                {
                    stubTimeProvider.IncrementMilliseconds(200);
                }

                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i >= 51 && i <= 55)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i <= 50 || i >= 56)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public void StartTimeType_FromCurrent_Common()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, TimeSpan.FromSeconds(1), StartTimeType.FromCurrent, 50, 0);

            stubTimeProvider.IncrementMilliseconds(800);

            for (int i = 1; i <= 100; i++)
            {
                stubTimeProvider.Increment();

                if (i == 56)
                {
                    stubTimeProvider.IncrementMilliseconds(200);
                }

                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i >= 51 && i <= 70)
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
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, TimeSpan.FromSeconds(1), StartTimeType.FromNaturalPeriodBeign, 50, 3);

            for (int i = 1; i <= 70; i++)
            {
                stubTimeProvider.Increment();

                if (i == 61 || i == 62 || i == 63)
                {
                    stubTimeProvider.IncrementMilliseconds(1000);
                }

                var result = processor.Check(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i >= 51 && i <= 62)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i <= 50 || i >= 63)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public void UpdateRules_NarrowTimeWindowExpired_CreateNewTimeWindow()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            FixedWindowRule[] fixedWindowRules = CreateRules(50, 1000);

            IAlgorithm algorithm = new InProcessFixedWindowAlgorithm(fixedWindowRules, stubTimeProvider, updatable: true);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 52)
                {
                    var fixedWindowRules2 = CreateRules(50, 500);
                    algorithm.UpdateRules(fixedWindowRules2);
                    stubTimeProvider.IncrementMilliseconds(500);
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
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            FixedWindowRule[] fixedWindowRules = CreateRules(50, 1000);

            IAlgorithm algorithm = new InProcessFixedWindowAlgorithm(fixedWindowRules, stubTimeProvider, updatable: true);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 52)
                {
                    var fixedWindowRules2 = CreateRules(50, 500);
                    algorithm.UpdateRules(fixedWindowRules2);
                }

                if (i == 61)
                {
                    stubTimeProvider.IncrementMilliseconds(200);
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
                    stubTimeProvider.IncrementMilliseconds(300);
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
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            FixedWindowRule[] fixedWindowRules = CreateRules(50, 1000);

            IAlgorithm algorithm = new InProcessFixedWindowAlgorithm(fixedWindowRules, stubTimeProvider, updatable: true);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 52)
                {
                    var fixedWindowRules2 = CreateRules(50, 2000);
                    algorithm.UpdateRules(fixedWindowRules2);
                }

                if (i == 61)
                {
                    stubTimeProvider.IncrementMilliseconds(1000);
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
                    stubTimeProvider.IncrementMilliseconds(1000);
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
        public void UpdateRules_RaiseLimitNumber_LoseLimit()
        {
            FixedWindowRule[] fixedWindowRules = CreateRules(50);

            IAlgorithm algorithm = new InProcessFixedWindowAlgorithm(fixedWindowRules, updatable: true);

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
        public void UpdateRules_ReduceLimitNumber_TriggerLimit()
        {
            FixedWindowRule[] fixedWindowRules = CreateRules(50);

            IAlgorithm algorithm = new InProcessFixedWindowAlgorithm(fixedWindowRules, updatable: true);

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
        public async Task CommonAsync()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, TimeSpan.FromMilliseconds(60), StartTimeType.FromCurrent, 50, 0);

            // 0-50 51-60 61-110 111-120 121-140
            for (int i = 1; i <= 140; i++)
            {
                stubTimeProvider.Increment();

                var result = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if ((i >= 51 && i <= 60) || (i >= 111 && i <= 120))
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i <= 50 || (i >= 61 && i <= 110) || i >= 121)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public async Task ResetTimeAsync_NotExistKey_ReturnTimeWindowAsExpireTimeAsync()
        {
            var now = DateTimeOffset.Now;
            var statWondow = TimeSpan.FromMilliseconds(60);
            var stubTimeProvider = new TestTimeProvider(now, TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, statWondow, StartTimeType.FromCurrent, 50, 0);

            var result = await processor.CheckAsync(new SimulationRequest()
            {
                RequestId = Guid.NewGuid().ToString(),
                RequestResource = "home",
                Parameters = new Dictionary<string, string>() { { "from", "sample" } }
            });

            var expected = now.Add(statWondow);
            Assert.AreEqual(expected, result.RuleCheckResults.First().ResetTime);
        }

        [DataTestMethod]
        public async Task ResetTimeAsync_ExistKey_ReturnFirstWindowExpireTimeAsync()
        {
            var now = DateTimeOffset.Now;
            var statWondow = TimeSpan.FromMilliseconds(60);
            var stubTimeProvider = new TestTimeProvider(now, TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, statWondow, StartTimeType.FromCurrent, 50, 0);

            for (int i = 0; i < 3; i++)
            {
                var result = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                var expected = now.Add(statWondow);
                Assert.AreEqual(expected, result.RuleCheckResults.First().ResetTime);

                stubTimeProvider.Increment();
            }
        }

        [DataTestMethod]
        public async Task ResetTimeAsync_ExistKey_ReturnNextWindowExpireTimeAsync()
        {
            var now = DateTimeOffset.Now;
            var statWondow = TimeSpan.FromMilliseconds(60);
            var stubTimeProvider = new TestTimeProvider(now, TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, statWondow, StartTimeType.FromCurrent, 50, 0);

            for (int i = 0; i < 3; i++)
            {
                if (i == 3)
                {
                    now = DateTimeOffset.Now;
                }

                var result = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i == 3)
                {
                    var expected = now.Add(statWondow);
                    Assert.AreEqual(expected, result.RuleCheckResults.First().ResetTime);
                }

                stubTimeProvider.IncrementMilliseconds(30);
            }
        }

        [DataTestMethod]
        public async Task ResetTimeAsync_TriggerLimit_ReturnLockExpireTimeAsync()
        {
            var now = DateTimeOffset.Now;
            var statWondow = TimeSpan.FromMilliseconds(60);
            var stubTimeProvider = new TestTimeProvider(now, TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, statWondow, StartTimeType.FromCurrent, 50, 3);

            for (int i = 0; i < 60; i++)
            {
                var result = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i >= 51)
                {
                    var expected = now.Add(TimeSpan.FromSeconds(3));
                    Assert.AreEqual(expected, result.RuleCheckResults.First().ResetTime);
                }
            }
        }

        [DataTestMethod]
        public async Task PeekAsync_NotExistKey_Common()
        {
            var now = DateTimeOffset.Now;
            var statWondow = TimeSpan.FromMilliseconds(60);
            var stubTimeProvider = new TestTimeProvider(now, TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, statWondow, StartTimeType.FromCurrent, 50, 0);

            var result = await processor.PeekAsync("home");
            var firstResult = result.RuleCheckResults.First();

            Assert.AreEqual(DateTimeOffset.MinValue, firstResult.ResetTime);
            Assert.AreEqual(0, firstResult.Count);
            Assert.AreEqual(false, firstResult.IsLimit);
        }

        [DataTestMethod]
        public async Task PeekAsync_ExistKey_Common()
        {
            var statWindow = TimeSpan.FromMilliseconds(65);
            var now = DateTimeOffset.Now;
            var lockSeconds = 1;
            var stubTimeProvider = new TestTimeProvider(now, TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, statWindow, StartTimeType.FromCurrent, 60, lockSeconds);
            DateTimeOffset expected = DateTimeOffset.MinValue;

            for (int i = 1; i < 70; i++)
            {
                var result = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i == 1)
                {
                    expected = now.Add(statWindow);
                }
                if (i == 61)
                {
                    expected = stubTimeProvider.GetCurrentLocalTime().AddSeconds(lockSeconds);
                }

                if (i <= 60)
                {
                    var peekResult = await processor.PeekAsync("home");
                    var firstPeekResult = peekResult.RuleCheckResults.First();
                    Assert.AreEqual(expected, firstPeekResult.ResetTime);
                    Assert.AreEqual(i, firstPeekResult.Count);
                    Assert.AreEqual(false, firstPeekResult.IsLimit);
                }

                if (i >= 61 && i <= 65)
                {
                    var peekResult = await processor.PeekAsync("home");
                    var firstPeekResult = peekResult.RuleCheckResults.First();
                    Assert.AreEqual(expected, firstPeekResult.ResetTime);
                    Assert.AreEqual(-1, firstPeekResult.Count);
                    Assert.AreEqual(true, firstPeekResult.IsLimit);
                }

                stubTimeProvider.Increment();
            }
        }

        [DataTestMethod]
        public async Task StartTimeTypeAsync_FromNaturalPeriodBeign_Common()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, TimeSpan.FromSeconds(1), StartTimeType.FromNaturalPeriodBeign, 50, 0);

            stubTimeProvider.IncrementMilliseconds(800);

            // 1-50 51-55 56-70
            for (int i = 1; i <= 70; i++)
            {
                stubTimeProvider.Increment();

                if (i == 56)
                {
                    stubTimeProvider.IncrementMilliseconds(200);
                }

                var result = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i >= 51 && i <= 55)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i <= 50 || i >= 56)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public async Task StartTimeTypeAsync_FromCurrent_Common()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, TimeSpan.FromSeconds(1), StartTimeType.FromCurrent, 50, 0);

            stubTimeProvider.IncrementMilliseconds(800);

            for (int i = 1; i <= 100; i++)
            {
                stubTimeProvider.Increment();

                if (i == 56)
                {
                    stubTimeProvider.IncrementMilliseconds(200);
                }

                var result = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i >= 51 && i <= 70)
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
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            var processor = GetAlgorithm(stubTimeProvider, TimeSpan.FromSeconds(1), StartTimeType.FromNaturalPeriodBeign, 50, 3);

            for (int i = 1; i <= 70; i++)
            {
                stubTimeProvider.Increment();

                if (i == 61 || i == 62 || i == 63)
                {
                    stubTimeProvider.IncrementMilliseconds(1000);
                }

                var result = await processor.CheckAsync(new SimulationRequest()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestResource = "home",
                    Parameters = new Dictionary<string, string>() { { "from", "sample" } }
                });

                if (i >= 51 && i <= 62)
                {
                    Assert.AreEqual(true, result.IsLimit);
                }

                if (i <= 50 || i >= 63)
                {
                    Assert.AreEqual(false, result.IsLimit);
                }
            }
        }

        [DataTestMethod]
        public async Task UpdateRulesAsync_RaiseLimitNumber_LoseLimit()
        {
            FixedWindowRule[] fixedWindowRules = CreateRules(50);

            IAlgorithm algorithm = new InProcessFixedWindowAlgorithm(fixedWindowRules, updatable: true);

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
        public async Task UpdateRulesAsync_ReduceLimitNumber_TriggerLimit()
        {
            FixedWindowRule[] fixedWindowRules = CreateRules(50);

            IAlgorithm algorithm = new InProcessFixedWindowAlgorithm(fixedWindowRules, updatable: true);

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
        public async Task UpdateRulesAsync_NarrowTimeWindowExpired_CreateNewTimeWindow()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            FixedWindowRule[] fixedWindowRules = CreateRules(50, 1000);

            IAlgorithm algorithm = new InProcessFixedWindowAlgorithm(fixedWindowRules, stubTimeProvider, updatable: true);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 52)
                {
                    var fixedWindowRules2 = CreateRules(50, 500);
                    algorithm.UpdateRules(fixedWindowRules2);
                    stubTimeProvider.IncrementMilliseconds(500);
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
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            FixedWindowRule[] fixedWindowRules = CreateRules(50, 1000);

            IAlgorithm algorithm = new InProcessFixedWindowAlgorithm(fixedWindowRules, stubTimeProvider, updatable: true);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 52)
                {
                    var fixedWindowRules2 = CreateRules(50, 500);
                    algorithm.UpdateRules(fixedWindowRules2);
                }

                if (i == 61)
                {
                    stubTimeProvider.IncrementMilliseconds(200);
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
                    stubTimeProvider.IncrementMilliseconds(300);
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
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            FixedWindowRule[] fixedWindowRules = CreateRules(50, 1000);

            IAlgorithm algorithm = new InProcessFixedWindowAlgorithm(fixedWindowRules, stubTimeProvider, updatable: true);

            for (int i = 1; i <= 70; i++)
            {
                if (i == 52)
                {
                    var fixedWindowRules2 = CreateRules(50, 2000);
                    algorithm.UpdateRules(fixedWindowRules2);
                }

                if (i == 61)
                {
                    stubTimeProvider.IncrementMilliseconds(1000);
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
                    stubTimeProvider.IncrementMilliseconds(1000);
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

        private IAlgorithm GetAlgorithm(ITimeProvider timeProvider, TimeSpan statWindow, StartTimeType startTimeType, int limitNumber, int lockSeconds)
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

            return new InProcessFixedWindowAlgorithm(fixedWindowRules, timeProvider);
        }
    }
}