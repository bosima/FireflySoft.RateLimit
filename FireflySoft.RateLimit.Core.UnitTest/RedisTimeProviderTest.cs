using System;
using System.Threading;
using System.Threading.Tasks;
using FireflySoft.RateLimit.Core.Time;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FireflySoft.RateLimit.Core.UnitTest
{
    [TestClass]
    public class RedisTimeProviderTest
    {
        [DataTestMethod]
        public void TestGetCurrentUtcTime()
        {
            var currentUtcTime = GetTimeProvider().GetCurrentUtcTime();
            Assert.AreEqual(true, currentUtcTime.Year >= 2021);
        }

        [DataTestMethod]
        public void TestGetCurrentUtcMilliseconds()
        {
            var currentTs = GetTimeProvider().GetCurrentUtcMilliseconds();
            Assert.AreEqual(true, currentTs > DateTimeOffset.Parse("2021-1-1").ToUnixTimeMilliseconds());
        }

        [DataTestMethod]
        public void TestGetCurrentLocalTime()
        {
            var localTime = GetTimeProvider().GetCurrentLocalTime();
            Assert.AreEqual(true, localTime.Year >= 2021);
        }

        [DataTestMethod]
        public async Task TestGetCurrentUtcTimeAsync()
        {
            var currentUtcTime = await GetTimeProvider().GetCurrentUtcTimeAsync();
            Assert.AreEqual(true, currentUtcTime.Offset == TimeSpan.FromHours(0));
            Assert.AreEqual(true, currentUtcTime.Year >= 2021);
        }

        [DataTestMethod]
        public async Task TestGetCurrentUtcMillisecondsAsync()
        {
            var currentTs = await GetTimeProvider().GetCurrentUtcMillisecondsAsync();
            Assert.AreEqual(true, currentTs > DateTimeOffset.Parse("2021-1-1").ToUnixTimeMilliseconds());
        }

        [DataTestMethod]
        public async Task TestGetCurrentLocalTimeAsync()
        {
            var localTime = await GetTimeProvider().GetCurrentLocalTimeAsync();
            Assert.AreEqual(true, localTime.Year >= 2021);
        }

        private ITimeProvider GetTimeProvider()
        {
            return new RedisTimeProvider(RedisClientHelper.GetClient());
        }
    }
}