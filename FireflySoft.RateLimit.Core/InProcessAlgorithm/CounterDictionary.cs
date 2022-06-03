using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FireflySoft.RateLimit.Core.Time;

namespace FireflySoft.RateLimit.Core.InProcessAlgorithm
{
    /// <summary>
    /// Define a dictionary for rate limiting counter
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CounterDictionary<T>
    {
        private DateTimeOffset _lastExpirationScan;

        private TimeSpan _expirationScanFrequency;

        private ITimeProvider _timeProvider;

        private ConcurrentDictionary<string, CounterDictionaryItem<T>> _items = new ConcurrentDictionary<string, CounterDictionaryItem<T>>();

        private ICollection<KeyValuePair<string, CounterDictionaryItem<T>>> _itemsCollection => _items;

        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="timeProvider"></param>
        public CounterDictionary(ITimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
            _lastExpirationScan = _timeProvider.GetCurrentLocalTime();
            _expirationScanFrequency = TimeSpan.FromSeconds(60);
        }

        /// <summary>
        /// Gets the count of this dictionary
        /// </summary>
        /// <returns></returns>
        public int GetCount()
        {
            var count = _items.Count;
            StartScanForExpiredItemsIfNeeded(_timeProvider.GetCurrentLocalTime());
            return count;
        }

        /// <summary>
        /// Set a counter
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        public void Set(string key, CounterDictionaryItem<T> item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item can not be null.");
            }

            var now = _timeProvider.GetCurrentLocalTime();

            if (item.ExpireTime <= now)
            {
                throw new InvalidDataException("ExpireTime must great than current time.");
            }

            if (_items.TryAdd(key, item))
            {
                StartScanForExpiredItemsIfNeeded(now);
                return;
            }

            if (_items.TryGetValue(key, out var oldItem))
            {
                if (item.IsExpired)
                {
                    item.IsExpired = false;
                }

                var updateResult = _items.TryUpdate(key, item, oldItem);

                // maybe removed by scan for expired items, so try add it
                if (!updateResult)
                {
                    _items.TryAdd(key, item);
                }
            }

            StartScanForExpiredItemsIfNeeded(now);
        }

        /// <summary>
        /// Get a counter item
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool TryGet(string key, out CounterDictionaryItem<T> item)
        {
            var now = _timeProvider.GetCurrentLocalTime();

            if (_items.TryGetValue(key, out item))
            {
                if (item.IsExpired)
                {
                    StartScanForExpiredItemsIfNeeded(now);
                    return false;
                }

                if (now >= item.ExpireTime)
                {
                    item.IsExpired = true;
                    StartScanForExpiredItemsIfNeeded(now);
                    return false;
                }

                StartScanForExpiredItemsIfNeeded(now);
                return true;
            }

            StartScanForExpiredItemsIfNeeded(now);
            return false;
        }

        // reference: https://github.com/dotnet/runtime/blob/1466e404dfac7ad6af7e6877d26885ce42414120/src/libraries/Microsoft.Extensions.Caching.Memory/src/MemoryCache.cs#L327
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StartScanForExpiredItemsIfNeeded(DateTimeOffset now)
        {
            if (_expirationScanFrequency < now - _lastExpirationScan)
            {
                ScheduleTask(now);
            }

            void ScheduleTask(DateTimeOffset now)
            {
                _lastExpirationScan = now;
                Task.Factory.StartNew(state => ((CounterDictionary<T>)state!).ScanForExpiredItems(), this,
                    CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }
        }

        private void ScanForExpiredItems()
        {
            DateTimeOffset now = _lastExpirationScan = _timeProvider.GetCurrentLocalTime();

            foreach (KeyValuePair<string, CounterDictionaryItem<T>> entry in _items)
            {
                var item = entry.Value;

                if (item.CheckExpired(now))
                {
                    _itemsCollection.Remove(new KeyValuePair<string, CounterDictionaryItem<T>>(item.Key, item));
                }
            }
        }
    }

    /// <summary>
    /// Define the item in the CounterDictionary
    /// </summary>
    public class CounterDictionaryItem<T>
    {
        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="key"></param>
        /// <param name="counter"></param>
        public CounterDictionaryItem(string key, T counter)
        {
            Key = key;
            Counter = counter;
        }

        internal bool IsExpired { get; set; }

        /// <summary>
        /// The key
        /// </summary>
        /// <value></value>
        public string Key { get; set; }

        /// <summary>
        /// The counter
        /// </summary>
        /// <value></value>
        public T Counter { get; set; }

        /// <summary>
        /// The expire time of this item
        /// </summary>
        /// <value></value>
        public DateTimeOffset ExpireTime { get; set; }

        internal bool CheckExpired(DateTimeOffset now)
        {
            return IsExpired || now > ExpireTime;
        }
    }
}