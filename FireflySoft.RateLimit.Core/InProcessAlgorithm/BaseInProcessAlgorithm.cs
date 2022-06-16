using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using FireflySoft.RateLimit.Core.Rule;
using FireflySoft.RateLimit.Core.Time;

namespace FireflySoft.RateLimit.Core.InProcessAlgorithm
{
    /// <summary>
    /// The base class for in process algorithm
    /// </summary>
    public abstract class BaseInProcessAlgorithm : BaseAlgorithm
    {
        readonly CounterDictionary<bool> _lockDictionary;

        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="rules">The rate limit rules</param>
        /// <param name="timeProvider">The time provider, it is a instance of LocalTimeProvider by default.</param>
        /// <param name="updatable">If rules can be updated</param>
        public BaseInProcessAlgorithm(IEnumerable<RateLimitRule> rules, ITimeProvider timeProvider = null, bool updatable = false)
        : base(rules, timeProvider, updatable)
        {
            _lockDictionary = new CounterDictionary<bool>(_timeProvider);
            _lockDictionary.Set("IPMS", new CounterDictionaryItem<bool>("IMPS", true)
            {
                ExpireTime = DateTimeOffset.MaxValue
            });
        }

        /// <summary>
        /// Lock the rate limit target until the expiration time, when triggering the rate limit rule.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="currentTime"></param>
        /// <param name="expireTimeSpan"></param>
        protected bool TryLock(string target, DateTimeOffset currentTime, TimeSpan expireTimeSpan)
        {
            var expireTime = currentTime.Add(expireTimeSpan);
            var key = $"{target}-lock";
            _lockDictionary.Set($"{target}-lock", new CounterDictionaryItem<bool>(key, true)
            {
                ExpireTime = expireTime
            });

            return true;
        }

        /// <summary>
        /// Lock the rate limit target until the expiration time, when triggering the rate limit rule.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="expireTime"></param>
        protected bool TryLock(string target, DateTimeOffset expireTime)
        {
            var key = $"{target}-lock";
            _lockDictionary.Set(key, new CounterDictionaryItem<bool>(key, true)
            {
                ExpireTime = expireTime
            });

            return true;
        }

        /// <summary>
        /// Check whether the rate limit target is locked
        /// </summary>
        /// <param name="target"></param>
        /// <param name="expireTime"></param>
        /// <returns></returns>
        protected bool CheckLocked(string target, out DateTimeOffset? expireTime)
        {
            expireTime = null;
            var key = $"{target}-lock";
            if (_lockDictionary.TryGet(key, out var item))
            {
                expireTime = item.ExpireTime;
                return item.Counter;
            }

            return false;
        }
    }
}