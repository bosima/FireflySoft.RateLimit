using System;
using System.Threading;
using System.Threading.Tasks;
using FireflySoft.RateLimit.Core.Time;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FireflySoft.RateLimit.Core.UnitTest
{
    [TestClass]
    public class LocalTimeProviderTest
    {
        [DataTestMethod]
        public void TestGetCurrentUtcTime()
        {
            var currentUtcTime = GetTimeProvider().GetCurrentUtcTime();
            Assert.AreEqual(true, currentUtcTime <= DateTimeOffset.UtcNow);
        }

        [DataTestMethod]
        public void TestGetCurrentUtcMilliseconds()
        {
            var currentTs = GetTimeProvider().GetCurrentUtcMilliseconds();
            Assert.AreEqual(true, currentTs <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        }

        [DataTestMethod]
        public void TestGetCurrentLocalTime()
        {
            var localTime = GetTimeProvider().GetCurrentLocalTime();
            Assert.AreEqual(true, localTime <= DateTimeOffset.Now);
        }

        [DataTestMethod]
        public async Task TestGetCurrentUtcTimeAsync()
        {
            var currentUtcTime = await GetTimeProvider().GetCurrentUtcTimeAsync();
            Assert.AreEqual(true, currentUtcTime.Offset == TimeSpan.FromHours(0));
            Assert.AreEqual(true, currentUtcTime <= DateTimeOffset.UtcNow);
        }

        [DataTestMethod]
        public async Task TestGetCurrentUtcMillisecondsAsync()
        {
            var currentTs = await GetTimeProvider().GetCurrentUtcMillisecondsAsync();
            Assert.AreEqual(true, currentTs <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        }

        [DataTestMethod]
        public async Task TestGetCurrentLocalTimeAsync()
        {
            var localTime = await GetTimeProvider().GetCurrentLocalTimeAsync();
            Assert.AreEqual(true, localTime <= DateTimeOffset.Now);
        }

        private ITimeProvider GetTimeProvider()
        {
            return new LocalTimeProvider();
        }
    }
}