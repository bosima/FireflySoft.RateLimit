using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// In-process Memory Storage
    /// </summary>
    public class InProcessMemoryStorage : IRateLimitStorage
    {
        readonly MemoryCache _cache;
        readonly object _cacheLocker;

        /// <summary>
        /// Create a new instance
        /// </summary>
        public InProcessMemoryStorage()
        {
            _cache = new MemoryCache("InProcessMemoryStorage-" + Guid.NewGuid().ToString());
            _cacheLocker = new object();
        }

        /// <summary>
        /// Lock the rate limit target until the expiration time, when triggering the rate limit rule.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="expireTimeSpan"></param>
        public void TryLock(string target, TimeSpan expireTimeSpan)
        {
            var expireTime = DateTimeOffset.Now.Add(expireTimeSpan);
            _cache.Add($"lock-{target}", 1, expireTime);
        }

        /// <summary>
        /// Check whether the rate limit target is locked
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool CheckLocked(string target)
        {
            return _cache.Get($"lock-{target}") == null ? false : true;
        }

        /// <summary>
        /// Get the current unified time
        /// </summary>
        /// <returns></returns>
        public long GetCurrentTime()
        {
            return DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// Adds a rate limit target to the storage by using the specified function if the target does not already exist. Returns the new value, or the existing value if the target exists.
        /// </summary>
        /// <param name="target">The target to add.</param>
        /// <param name="retrieveMethod">The function used to generate a value for the target.</param>
        /// <returns></returns>
        public long GetOrAdd(string target, Lazy<long> retrieveMethod)
        {
            var cachedValue = _cache.AddOrGetExisting(target, retrieveMethod.Value, DateTimeOffset.MaxValue);
            if (cachedValue != null)
            {
                return (long)cachedValue;
            }
            return retrieveMethod.Value;
        }

        /// <summary>
        /// Gets the sum of the counts of multiple rate limit targets
        /// </summary>
        /// <param name="targets">The targets</param>
        /// <returns></returns>
        public long Sum(IEnumerable<string> targets)
        {
            var values = _cache.GetValues(targets);
            if (values != null && values.Count > 0)
            {
                return values.Where(d => d.Value != null).Select(d => ((CountValue)d.Value).Value).Sum();
            }

            return 0;
        }

        /// <summary>
        /// Increase the count value of the rate limit target. When the target does not exist, create it first and increase the specified value, then set its expiration time.
        /// </summary>
        /// <param name="target">The target</param>
        /// <param name="amount">amount of increase</param>
        /// <param name="expireTimeSpan">The expiration time is set when the target is created</param>
        /// <returns>amount of requests</returns>
        public long SimpleIncrement(string target, long amount, TimeSpan expireTimeSpan)
        {
            lock (_cacheLocker)
            {
                var result = _cache.GetCacheItem(target);
                if (result != null)
                {
                    var countValue = (CountValue)result.Value;
                    countValue.Value += amount;
                    return countValue.Value;
                }

                DateTimeOffset expireTime;
                if (expireTimeSpan == TimeSpan.Zero)
                {
                    expireTime = DateTimeOffset.MaxValue;
                }
                else
                {
                    expireTime = DateTimeOffset.Now.Add(expireTimeSpan);
                }

                _cache.Add(target, new CountValue(amount), expireTime);
                return amount;
            }
        }

        /// <summary>
        /// Increase the count value of the rate limit target for leaky bucket algorithm.
        /// </summary>
        /// <param name="target">The target</param>
        /// <param name="amount">amount of increase</param>
        /// <param name="capacity">The capacity of leaky bucket</param>
        /// <param name="outflowUnit">The time unit of outflow from the leaky bucket</param>
        /// <param name="outflowQuantityPerUnit">The outflow quantity per unit time</param>
        /// <returns>Amount of request in the bucket</returns>
        public long LeakyBucketIncrement(string target, long amount, long capacity, int outflowUnit, int outflowQuantityPerUnit)
        {
            lock (_cacheLocker)
            {
                var now = DateTimeOffset.Now;

                var result = _cache.GetCacheItem(target);
                if (result != null)
                {
                    var countValue = (CountValue)result.Value;
                    var lastTime = countValue.LastFlowTime;
                    var pastTime = now - lastTime;
                    var pastTimeMilliseconds = pastTime.TotalMilliseconds;
                    if (pastTimeMilliseconds < outflowUnit)
                    {
                        countValue.Value += amount;
                        return countValue.Value;
                    }

                    var pastOutflowUnitQuantity = (int)(pastTimeMilliseconds / outflowUnit);
                    var newLastTime = lastTime.AddMilliseconds(pastOutflowUnitQuantity * outflowUnit);
                    countValue.LastFlowTime = newLastTime;

                    var pastOutflowQuantity = outflowQuantityPerUnit * pastOutflowUnitQuantity;
                    var newCount = (countValue.Value > capacity ? capacity : countValue.Value) - pastOutflowQuantity + amount;
                    countValue.Value = newCount > 0 ? newCount : amount;

                    return countValue.Value;
                }

                _cache.Add(target, new CountValue(amount) { LastFlowTime = now }, DateTimeOffset.MaxValue);
                return amount;
            }
        }

        /// <summary>
        /// Decrease the count value of the rate limit target for token bucket algorithm.
        /// </summary>
        /// <param name="target">The target</param>
        /// <param name="amount">amount of decrease</param>
        /// <param name="capacity">The capacity of token bucket</param>
        /// <param name="inflowUnit">The time unit of inflow to the bucket bucket</param>
        /// <param name="inflowQuantityPerUnit">The inflow quantity per unit time</param>
        /// <returns>Amount of token in the bucket</returns>
        public long TokenBucketDecrement(string target, long amount, long capacity, int inflowUnit, int inflowQuantityPerUnit)
        {
            lock (_cacheLocker)
            {
                var now = DateTimeOffset.Now;

                var result = _cache.GetCacheItem(target);
                if (result != null)
                {
                    var countValue = (CountValue)result.Value;
                    var lastTime = countValue.LastFlowTime;
                    var pastTime = now - lastTime;
                    var pastTimeMilliseconds = pastTime.TotalMilliseconds;
                    if (pastTimeMilliseconds < inflowUnit)
                    {
                        countValue.Value -= amount;
                        return countValue.Value;
                    }

                    var pastInflowUnitQuantity = (int)(pastTimeMilliseconds / inflowUnit);
                    var newLastTime = lastTime.AddMilliseconds(pastInflowUnitQuantity * inflowUnit);
                    countValue.LastFlowTime = newLastTime;

                    var pastInflowQuantity = inflowQuantityPerUnit * pastInflowUnitQuantity;
                    var newCount = (countValue.Value < 0 ? 0 : countValue.Value) + pastInflowQuantity - amount;
                    if (newCount >= capacity)
                    {
                        countValue.Value = capacity - amount;
                    }
                    else
                    {
                        countValue.Value = newCount;
                    }

                    return countValue.Value;
                }

                var bucketAmount = capacity - amount;
                _cache.Add(target, new CountValue(bucketAmount) { LastFlowTime = now }, DateTimeOffset.MaxValue);
                return bucketAmount;
            }
        }

        private class CountValue
        {
            public CountValue(long value)
            {
                Value = value;
            }

            public long Value { get; set; }

            public DateTimeOffset LastFlowTime { get; set; }
        }
    }
}
