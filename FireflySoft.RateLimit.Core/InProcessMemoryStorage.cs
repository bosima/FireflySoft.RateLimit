using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// 进程内内存存储
    /// </summary>
    public class InProcessMemoryStorage : IRateLimitStorage
    {
        readonly MemoryCache _cache;
        readonly object _cacheLocker;

        public InProcessMemoryStorage()
        {
            _cache = new MemoryCache("InProcessMemoryStorage-" + Guid.NewGuid().ToString());
            _cacheLocker = new object();
        }

        public void TryLock(string target, TimeSpan expireTimeSpan)
        {
            var expireTime = DateTimeOffset.Now.Add(expireTimeSpan);
            _cache.Add($"lock-{target}", 1, expireTime);
        }

        public bool CheckLocked(string target)
        {
            return _cache.Get($"lock-{target}") == null ? false : true;
        }

        public long GetCurrentTime()
        {
            return DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        public long GetOrAdd(string target, Lazy<long> retrieveMethod)
        {
            var cachedValue = _cache.AddOrGetExisting(target, retrieveMethod.Value, DateTimeOffset.MaxValue);
            if (cachedValue != null)
            {
                return (long)cachedValue;
            }
            return retrieveMethod.Value;
        }

        public long Get(string target)
        {
            var result = _cache.GetCacheItem(target);
            if (result != null)
            {
                var countValue = (CountValue)result.Value;
                return countValue.Value;
            }
            return -1;
        }

        public long MGet(IEnumerable<string> targets)
        {
            var values = _cache.GetValues(targets);
            if (values != null && values.Count > 0)
            {
                return values.Where(d => d.Value != null).Select(d => ((CountValue)d.Value).Value).Sum();
            }

            return 0;
        }

        public long Increment(string target, long amount, TimeSpan expireTimeSpan)
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
