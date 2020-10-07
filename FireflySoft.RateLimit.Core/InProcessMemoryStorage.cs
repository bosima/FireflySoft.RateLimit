using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Caching;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// 进程内内存存储
    /// </summary>
    public class InProcessMemoryStorage : IRateLimitStorage
    {
        // 使用Memory可以自动移除过期的缓存，而不用自己实现这个逻辑
        readonly MemoryCache _cache;
        readonly object _cacheLocker;

        public InProcessMemoryStorage()
        {
            _cache = new MemoryCache("InProcessMemoryStorage-" + Guid.NewGuid().ToString());
            _cacheLocker = new object();
        }

        /// <summary>
        /// 锁定目标
        /// </summary>
        /// <param name="target"></param>
        /// <param name="expireTimeSpan"></param>
        public void Lock(string target, TimeSpan expireTimeSpan)
        {
            var expireTime = DateTimeOffset.Now.Add(expireTimeSpan);
            _cache.Add($"lock-{target}", 1, expireTime);
        }

        /// <summary>
        /// 检查目标是否被锁定
        /// </summary>
        /// <param name="target"></param>
        public bool CheckIsLocked(string target)
        {
            return _cache.Get($"lock-{target}") == null ? false : true;
        }

        /// <summary>
        /// 增加限流目标的统计值，并在首次创建时设置过期时间
        /// </summary>
        /// <param name="target"></param>
        /// <param name="amount"></param>
        /// <param name="expireTimeSpan"></param>
        /// <returns></returns>
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

                var expireTime = DateTimeOffset.Now.Add(expireTimeSpan);
                _cache.Add(target, new CountValue(amount), expireTime);
                return amount;
            }
        }

        private class CountValue
        {
            public CountValue(long value)
            {
                Value = value;
            }

            public long Value { get; set; }
        }
    }
}
