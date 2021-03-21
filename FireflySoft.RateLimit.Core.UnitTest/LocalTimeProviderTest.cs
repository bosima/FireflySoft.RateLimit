using System;
using System.Threading.Tasks;
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
        public void TestGetCurrentUnixTimeMilliseconds()
        {
            var currentTs = GetTimeProvider().GetCurrentUnixTimeMilliseconds();
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
            Assert.AreEqual(true, currentUtcTime <= DateTimeOffset.UtcNow);
        }

        [DataTestMethod]
        public async Task TestGetCurrentUnixTimeMillisecondsAsync()
        {
            var currentTs = await GetTimeProvider().GetCurrentUnixTimeMillisecondsAsync();
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