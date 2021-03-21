using System;
using System.Threading.Tasks;
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
        public void TestGetCurrentUnixTimeMilliseconds()
        {
            var currentTs = GetTimeProvider().GetCurrentUnixTimeMilliseconds();
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
            Assert.AreEqual(true, currentUtcTime.Year >= 2021);
        }

        [DataTestMethod]
        public async Task TestGetCurrentUnixTimeMillisecondsAsync()
        {
            var currentTs = await GetTimeProvider().GetCurrentUnixTimeMillisecondsAsync();
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
            return new RedisTimeProvider(StackExchange.Redis.ConnectionMultiplexer.Connect("127.0.0.1"));
        }
    }
}