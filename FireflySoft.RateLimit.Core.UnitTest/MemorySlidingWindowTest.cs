using System;
using FireflySoft.RateLimit.Core.InProcessAlgorithm;
using FireflySoft.RateLimit.Core.Rule;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FireflySoft.RateLimit.Core.UnitTest
{
    [TestClass]
    public class MemorySlidingWindowTest
    {
        [DataTestMethod]
        public void Increament_SameSeconds_ReturnSamePeriodIndex()
        {
            var rule = new SlidingWindowRule(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1));
            MemorySlidingWindow slidingWindow = new MemorySlidingWindow(rule);
            for (int i = 1; i <= 1000; i++)
            {
                var periodIndex = slidingWindow.LoadPeriod(1637459743000);
                Assert.AreEqual(0, periodIndex);
            }
        }

        [DataTestMethod]
        public void Increament_MultiSeconds_ReturnNormalPeriodIndexPerSecond()
        {
            var rule = new SlidingWindowRule(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1));
            MemorySlidingWindow slidingWindow = new MemorySlidingWindow(rule);

            long startTime = 1637459743000;
            for (int j = 1; j <= 10; j++)
            {
                for (int i = 1; i <= 1000; i++)
                {
                    var periodIndex = slidingWindow.LoadPeriod(startTime);
                    Assert.AreEqual(j - 1, periodIndex);
                }

                startTime += 1000;
            }
        }

        [DataTestMethod]
        public void Increament_SameSeconds_ReturnNormalAccum()
        {
            var rule = new SlidingWindowRule(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1));
            MemorySlidingWindow slidingWindow = new MemorySlidingWindow(rule);
            for (int i = 1; i <= 1000; i++)
            {
                var periodIndex = slidingWindow.LoadPeriod(1637459743000);
                var countValue = slidingWindow.IncreamentPeriod(periodIndex, 1);
                Assert.AreEqual(i, countValue);
            }
        }

        [DataTestMethod]
        public void Increament_SameSeconds_ReturnNormalCountValue()
        {
            var rule = new SlidingWindowRule(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1));
            MemorySlidingWindow slidingWindow = new MemorySlidingWindow(rule);
            for (int i = 1; i <= 1000; i++)
            {
                var periodIndex = slidingWindow.LoadPeriod(1637459743000);
                slidingWindow.IncreamentPeriod(periodIndex, 1);
            }

            var countValue = slidingWindow.GetCount();
            Assert.AreEqual(1000, countValue);
        }

        [DataTestMethod]
        public void Increament_MultiSeconds_ReturnNormalAccumPerSecond()
        {
            var rule = new SlidingWindowRule(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1));
            MemorySlidingWindow slidingWindow = new MemorySlidingWindow(rule);

            long startTime = 1637459743000;
            for (int j = 1; j <= 10; j++)
            {
                for (int i = 1; i <= 1000; i++)
                {
                    var periodIndex = slidingWindow.LoadPeriod(startTime);
                    var countValue = slidingWindow.IncreamentPeriod(periodIndex, 1);
                    Assert.AreEqual(i, countValue);
                }

                startTime += 1000;
            }
        }

        [DataTestMethod]
        public void Increament_MultiSeconds_ReturnNormalCountValue()
        {
            var rule = new SlidingWindowRule(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1));
            MemorySlidingWindow slidingWindow = new MemorySlidingWindow(rule);

            long startTime = 1637459743000;
            for (int j = 1; j <= 10; j++)
            {
                for (int i = 1; i <= 1000; i++)
                {
                    var periodIndex = slidingWindow.LoadPeriod(startTime);
                    slidingWindow.IncreamentPeriod(periodIndex, 1);
                }

                startTime += 1000;
            }

            var countValue = slidingWindow.GetCount();
            Assert.AreEqual(10000, countValue);
        }

        [DataTestMethod]
        public void Increament_SkipMultiSeconds_ReturnNormalCountValue()
        {
            var rule = new SlidingWindowRule(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1));
            MemorySlidingWindow slidingWindow = new MemorySlidingWindow(rule);

            long startTime = 1637459743000;
            for (int j = 1; j <= 15; j++)
            {
                if (j <= 10)
                {
                    for (int i = 1; i <= 1000; i++)
                    {
                        var periodIndex = slidingWindow.LoadPeriod(startTime);
                        slidingWindow.IncreamentPeriod(periodIndex, 1);
                    }
                }
                else
                {
                    for (int i = 1; i <= 500; i++)
                    {
                        var periodIndex = slidingWindow.LoadPeriod(startTime);
                        slidingWindow.IncreamentPeriod(periodIndex, 1);
                    }
                }

                startTime += 1000;
            }

            var countValue = slidingWindow.GetCount();
            Assert.AreEqual(7500, countValue);
        }
    }
}
