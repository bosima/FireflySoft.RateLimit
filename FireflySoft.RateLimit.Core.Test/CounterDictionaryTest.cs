using System;
using System.Threading;
using System.Threading.Tasks;
using FireflySoft.RateLimit.Core.InProcessAlgorithm;
using FireflySoft.RateLimit.Core.Time;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FireflySoft.RateLimit.Core.Test
{
    [TestClass]
    public class CounterDictionaryTest
    {
        [DataTestMethod]
        public void TryGet_ExpiredItem_ReturnFalse()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            CounterDictionary<string> dic = new CounterDictionary<string>(stubTimeProvider);

            dic.Set("key", new CounterDictionaryItem<string>("key", "value")
            {
                ExpireTime = DateTimeOffset.Parse("2022-01-01T00:00:20+00:00")
            });
            stubTimeProvider.IncrementSeconds(21);

            bool result = dic.TryGet("key", out CounterDictionaryItem<string> value);

            // run ScanForExpiredItems
            Thread.Sleep(10);

            Assert.AreEqual(false, result);
        }

        [DataTestMethod]
        public void TryGet_NotExpiredItem_ReturnTrue()
        {
            var stubTimeProvider = new TestTimeProvider(TimeSpan.FromMilliseconds(1));
            CounterDictionary<string> dic = new CounterDictionary<string>(stubTimeProvider);

            dic.Set("key", new CounterDictionaryItem<string>("key", "value")
            {
                ExpireTime = DateTimeOffset.Parse("2022-01-01T00:00:20+00:00")
            });
            stubTimeProvider.IncrementSeconds(10);

            bool result = dic.TryGet("key", out CounterDictionaryItem<string> value);

            Assert.AreEqual(true, result);
        }
    }
}