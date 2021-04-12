using System;
using FireflySoft.RateLimit.Core.Time;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FireflySoft.RateLimit.Core.UnitTest
{
    [TestClass]
    public class AlgorithmStartTimeTest
    {
        [DataTestMethod]
        public void TestToNaturalPeriodBeignTime()
        {
            DateTimeOffset startTime = DateTimeOffset.Parse("2021-12-21 21:21:21.211");

            var startTime1 = AlgorithmStartTime.ToNaturalPeriodBeignTime(startTime, TimeSpan.Parse("1"));
            Assert.AreEqual("2021-12-21 00:00:00.000", startTime1.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            var startTime2 = AlgorithmStartTime.ToNaturalPeriodBeignTime(startTime, TimeSpan.Parse("0.01:00:00"));
            Assert.AreEqual("2021-12-21 21:00:00.000", startTime2.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            var startTime3 = AlgorithmStartTime.ToNaturalPeriodBeignTime(startTime, TimeSpan.Parse("0.00:01:00"));
            Assert.AreEqual("2021-12-21 21:21:00.000", startTime3.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            var startTime4 = AlgorithmStartTime.ToNaturalPeriodBeignTime(startTime, TimeSpan.Parse("0.00:00:01"));
            Assert.AreEqual("2021-12-21 21:21:21.000", startTime4.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        }

        [DataTestMethod]
        public void TestToSpecifiedTypeTime()
        {
            DateTimeOffset startTime = DateTimeOffset.Parse("2021-12-21 21:21:21.211");
            long startTimeTs = startTime.ToUnixTimeMilliseconds();

            var startTime1 = AlgorithmStartTime.ToSpecifiedTypeTime(startTime, TimeSpan.Parse("1"), StartTimeType.FromCurrent);
            Assert.AreEqual("2021-12-21 21:21:21.211", startTime1.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            var startTime2 = AlgorithmStartTime.ToSpecifiedTypeTime(startTime, TimeSpan.Parse("1"), StartTimeType.FromNaturalPeriodBeign);
            Assert.AreEqual("2021-12-21 00:00:00.000", startTime2.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            var startTime3 = AlgorithmStartTime.ToSpecifiedTypeTime(startTimeTs, TimeSpan.Parse("1"), StartTimeType.FromCurrent);
            Assert.AreEqual(startTimeTs, startTime3);

            var startTime4 = AlgorithmStartTime.ToSpecifiedTypeTime(startTimeTs, TimeSpan.Parse("1"), StartTimeType.FromNaturalPeriodBeign);
            Assert.AreEqual(1640044800000, startTime4);
        }
    }
}