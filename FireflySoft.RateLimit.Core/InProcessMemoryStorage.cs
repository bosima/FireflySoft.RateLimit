using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// In-process Memory Storage
    /// </summary>
    public class InProcessMemoryStorage : IRateLimitStorage
    {
        readonly MemoryCache _cache;

        bool hasSetStartTime = false;

        ITimeProvider _timeProvider;

        /// <summary>
        /// Create a new instance
        /// </summary>
        public InProcessMemoryStorage()
        {
            _cache = new MemoryCache("IPMS-" + Guid.NewGuid().ToString());
            _cache.Add("IPMS", 1, DateTimeOffset.MaxValue);
        }

        /// <summary>
        /// Increase the count value of the rate limit target. When the target does not exist, create it first and increase the specified value, then set its expiration time.
        /// </summary>
        /// <param name="target">The target</param>
        /// <param name="amount">amount of increase</param>
        /// <param name="statWindow">Statistical time window</param>
        /// <param name="startTimeType">The type of starting time of statistical time period</param>
        /// <param name="limitNumber">The number of rate limit</param>
        /// <param name="lockSeconds">The number of seconds locked after triggering rate limiting. 0 means not locked</param>
        /// <returns>amount of requests</returns>
        public Tuple<bool, long> FixedWindowIncrement(string target, long amount, TimeSpan statWindow, StartTimeType startTimeType, int limitNumber, int lockSeconds)
        {
            bool locked = CheckLocked(target);
            if (locked)
            {
                return new Tuple<bool, long>(true, -1);
            }

            var startTime = CalculateStartTime(statWindow, startTimeType, GetCurrentLocalTime());

            Tuple<bool, long> incrementResult;
            lock (target)
            {
                DateTimeOffset expireTime = startTime.Add(statWindow);
                incrementResult = SimpleIncrement(target, amount, expireTime, limitNumber);
            }

            var checkResult = false;
            if (incrementResult.Item1)
            {
                checkResult = true;
                if (lockSeconds > 0)
                {
                    TryLock(target, TimeSpan.FromSeconds(lockSeconds));
                    return new Tuple<bool, long>(true, incrementResult.Item2);
                }
            }

            return new Tuple<bool, long>(checkResult, incrementResult.Item2);
        }

        /// <summary>
        /// Increase the count value of the rate limit target. When the target does not exist, create it first and increase the specified value, then set its expiration time.
        /// </summary>
        /// <param name="target">The target</param>
        /// <param name="amount">amount of increase</param>
        /// <param name="statWindow">Statistical time window</param>
        /// <param name="startTimeType">The type of starting time of statistical time period</param>
        /// <param name="limitNumber">The number of rate limit</param>
        /// <param name="lockSeconds">The number of seconds locked after triggering rate limiting. 0 means not locked</param>
        /// <returns>amount of requests</returns>
        public async Task<Tuple<bool, long>> FixedWindowIncrementAsync(string target, long amount, TimeSpan statWindow, StartTimeType startTimeType, int limitNumber, int lockSeconds)
        {
            return await Task.FromResult(FixedWindowIncrement(target, amount, statWindow, startTimeType, limitNumber, lockSeconds));
        }

        /// <summary>
        /// Increase the count value of the rate limit target for sliding window algorithm.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="amount"></param>
        /// <param name="statWindow"></param>
        /// <param name="statPeriod"></param>
        /// <param name="startTimeType">The type of starting time of statistical time period</param>
        /// <param name="periodNumber"></param>
        /// <param name="limitNumber">The number of rate limit</param>
        /// <param name="lockSeconds">The number of seconds locked after triggering rate limiting. 0 means not locked</param>
        /// <returns></returns>
        public Tuple<bool, long> SlidingWindowIncrement(string target, long amount, TimeSpan statWindow, TimeSpan statPeriod, StartTimeType startTimeType, int periodNumber, int limitNumber, int lockSeconds)
        {
            bool locked = CheckLocked(target);
            if (locked)
            {
                return new Tuple<bool, long>(true, -1);
            }

            // get current time
            var currentTime = GetCurrentLocalTime();

            // get the start time
            long startMilliseconds = GetOrSetStartTime(target, statWindow, startTimeType, currentTime);

            // get the stat periods
            var currentMilliseconds = currentTime.ToUnixTimeMilliseconds();
            var statPeriodArray = GetStatWindowPeriodArray(currentMilliseconds, startMilliseconds, periodNumber, (long)statPeriod.TotalMilliseconds);

            lock (target)
            {
                // get the total amount of all stat periods
                var totalAmount = Sum(statPeriodArray.Select(d => $"{target}-{d}").ToList()) + amount;

                if (limitNumber > 0 && totalAmount > limitNumber)
                {
                    if (lockSeconds > 0)
                    {
                        TryLock(target, TimeSpan.FromSeconds(lockSeconds));
                    }
                    return new Tuple<bool, long>(true, totalAmount);
                }

                // increment for current period
                var currentPeriod = statPeriodArray[0];
                var expireTime = currentTime.Add(statWindow).AddSeconds(3); // add 3s for: avoid data loss because other steps take too much time
                var incrementKey = $"{target}-{currentPeriod}";
                SimpleIncrement(incrementKey, amount, expireTime);

                return Tuple.Create(false, totalAmount);
            }
        }

        /// <summary>
        /// Increase the count value of the rate limit target for sliding window algorithm.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="amount"></param>
        /// <param name="statWindow"></param>
        /// <param name="statPeriod"></param>
        /// <param name="startTimeType">The type of starting time of statistical time period</param>
        /// <param name="periodNumber"></param>
        /// <param name="limitNumber">The number of rate limit</param>
        /// <param name="lockSeconds">The number of seconds locked after triggering rate limiting. 0 means not locked</param>
        /// <returns></returns>
        public async Task<Tuple<bool, long>> SlidingWindowIncrementAsync(string target, long amount, TimeSpan statWindow, TimeSpan statPeriod, StartTimeType startTimeType, int periodNumber, int limitNumber, int lockSeconds)
        {
            return await Task.FromResult(SlidingWindowIncrement(target, amount, statWindow, statPeriod, startTimeType, periodNumber, limitNumber, lockSeconds));
        }

        /// <summary>
        /// Increase the count value of the rate limit target for leaky bucket algorithm.
        /// </summary>
        /// <param name="target">The target</param>
        /// <param name="amount">amount of increase</param>
        /// <param name="capacity">The capacity of leaky bucket</param>
        /// <param name="outflowUnit">The time unit of outflow from the leaky bucket</param>
        /// <param name="outflowQuantityPerUnit">The outflow quantity per unit time</param>
        /// <param name="startTimeType">The type of starting time of 'outflowUnit'</param>
        /// <param name="lockSeconds">The number of seconds locked after triggering rate limiting. 0 means not locked</param>
        /// <returns>Amount of request in the bucket</returns>
        public Tuple<bool, long> LeakyBucketIncrement(string target, long amount, long capacity, int outflowUnit, int outflowQuantityPerUnit, StartTimeType startTimeType, int lockSeconds)
        {
            bool locked = CheckLocked(target);
            if (locked)
            {
                return new Tuple<bool, long>(true, -1);
            }

            lock (target)
            {
                var currentTime = GetCurrentLocalTime();

                var result = _cache.GetCacheItem(target);
                if (result == null)
                {
                    var startTime = CalculateStartTime(TimeSpan.FromMilliseconds(outflowUnit), startTimeType, currentTime);
                    _cache.Add(target, new CountValue(amount) { LastFlowTime = startTime }, DateTimeOffset.MaxValue);
                    return new Tuple<bool, long>(false, amount);
                }

                var countValue = (CountValue)result.Value;
                var lastTime = countValue.LastFlowTime;
                var pastTime = currentTime - lastTime;
                var lastTimeChanged = false;
                var pastTimeMilliseconds = pastTime.TotalMilliseconds;
                long newCount = 0;
                if (pastTimeMilliseconds < outflowUnit)
                {
                    newCount = countValue.Value + amount;
                }
                else
                {
                    var pastOutflowUnitQuantity = (int)(pastTimeMilliseconds / outflowUnit);
                    lastTime = lastTime.AddMilliseconds(pastOutflowUnitQuantity * outflowUnit);
                    lastTimeChanged = true;
                    var pastOutflowQuantity = outflowQuantityPerUnit * pastOutflowUnitQuantity;
                    newCount = countValue.Value - pastOutflowQuantity + amount;
                    newCount = newCount > 0 ? newCount : amount;
                }

                if (newCount > capacity)
                {
                    if (lockSeconds > 0)
                    {
                        TryLock(target, TimeSpan.FromSeconds(lockSeconds));
                    }
                    return new Tuple<bool, long>(true, newCount);
                }

                countValue.Value = newCount;
                if (lastTimeChanged)
                {
                    countValue.LastFlowTime = lastTime;
                }
                return new Tuple<bool, long>(false, countValue.Value);
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
        /// <param name="startTimeType">The type of starting time of 'outflowUnit'</param>
        /// <param name="lockSeconds">The number of seconds locked after triggering rate limiting. 0 means not locked</param>
        /// <returns>Amount of request in the bucket</returns>
        public async Task<Tuple<bool, long>> LeakyBucketIncrementAsync(string target, long amount, long capacity, int outflowUnit, int outflowQuantityPerUnit, StartTimeType startTimeType, int lockSeconds)
        {
            return await Task.FromResult(LeakyBucketIncrement(target, amount, capacity, outflowUnit, outflowQuantityPerUnit, startTimeType, lockSeconds));
        }

        /// <summary>
        /// Decrease the count value of the rate limit target for token bucket algorithm.
        /// </summary>
        /// <param name="target">The target</param>
        /// <param name="amount">amount of decrease</param>
        /// <param name="capacity">The capacity of token bucket</param>
        /// <param name="inflowUnit">The time unit of inflow to the bucket bucket</param>
        /// <param name="inflowQuantityPerUnit">The inflow quantity per unit time</param>
        /// <param name="startTimeType">The type of starting time of 'inflowUnit'</param>
        /// <param name="lockSeconds">The number of seconds locked after triggering rate limiting. 0 means not locked</param>
        /// <returns>Amount of token in the bucket</returns>
        public Tuple<bool, long> TokenBucketDecrement(string target, long amount, long capacity, int inflowUnit, int inflowQuantityPerUnit, StartTimeType startTimeType, int lockSeconds)
        {
            bool locked = CheckLocked(target);
            if (locked)
            {
                return new Tuple<bool, long>(true, -1);
            }

            lock (target)
            {
                var currentTime = GetCurrentLocalTime();
                long bucketAmount = 0;
                var result = _cache.GetCacheItem(target);
                if (result == null)
                {
                    var startTime = CalculateStartTime(TimeSpan.FromMilliseconds(inflowUnit), startTimeType, currentTime);
                    bucketAmount = capacity - amount;
                    _cache.Add(target, new CountValue(bucketAmount) { LastFlowTime = startTime }, DateTimeOffset.MaxValue);
                    return new Tuple<bool, long>(false, bucketAmount);
                }

                var countValue = (CountValue)result.Value;
                var lastTime = countValue.LastFlowTime;
                var lastTimeChanged = false;
                var pastTime = currentTime - lastTime;
                var pastTimeMilliseconds = pastTime.TotalMilliseconds;
                if (pastTimeMilliseconds < inflowUnit)
                {
                    bucketAmount = countValue.Value - amount;
                }
                else
                {
                    var pastInflowUnitQuantity = (int)(pastTimeMilliseconds / inflowUnit);
                    lastTime = lastTime.AddMilliseconds(pastInflowUnitQuantity * inflowUnit);
                    lastTimeChanged = true;
                    var pastInflowQuantity = inflowQuantityPerUnit * pastInflowUnitQuantity;
                    bucketAmount = (countValue.Value < 0 ? 0 : countValue.Value) + pastInflowQuantity - amount;
                }

                if (bucketAmount < 0)
                {
                    if (lockSeconds > 0)
                    {
                        TryLock(target, TimeSpan.FromSeconds(lockSeconds));
                    }

                    return new Tuple<bool, long>(true, bucketAmount);
                }

                if (bucketAmount >= capacity)
                {
                    bucketAmount = capacity - amount;
                }

                countValue.Value = bucketAmount;
                if (lastTimeChanged)
                {
                    countValue.LastFlowTime = lastTime;
                }
                return new Tuple<bool, long>(false, countValue.Value);
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
        /// <param name="startTimeType">The type of starting time of 'inflowUnit'</param>
        /// <param name="lockSeconds">The number of seconds locked after triggering rate limiting. 0 means not locked</param>
        /// <returns>Amount of token in the bucket</returns>
        public async Task<Tuple<bool, long>> TokenBucketDecrementAsync(string target, long amount, long capacity, int inflowUnit, int inflowQuantityPerUnit, StartTimeType startTimeType, int lockSeconds)
        {
            return await Task.FromResult(TokenBucketDecrement(target, amount, capacity, inflowUnit, inflowQuantityPerUnit,startTimeType, lockSeconds));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeProvider"></param>
        public void SetTimeProvider(ITimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
        }

        private List<long> GetStatWindowPeriodArray(long currentMilliseconds, long startMilliseconds, int periodNumber, long statPeriodMilliseconds)
        {
            var currentPeriod = GetCurrentPeriod(currentMilliseconds, startMilliseconds, statPeriodMilliseconds);
            var periodSet = new List<long>() { currentPeriod };
            for (int i = 1; i < periodNumber; i++)
            {
                periodSet.Add(currentPeriod - statPeriodMilliseconds * i);
            }

            return periodSet;
        }

        private long GetCurrentPeriod(long currentMilliseconds, long startMilliseconds, long statPeriodMilliseconds)
        {
            var pastTotalMilliseconds = currentMilliseconds - startMilliseconds;
            var pastPeriodNumber = pastTotalMilliseconds / (double)statPeriodMilliseconds;
            var pastPeriodNumberFloor = (long)Math.Floor(pastPeriodNumber);
            var pastPeriodNumberCeiling = (long)Math.Ceiling(pastPeriodNumber);
            long pastPeriodNumberFixed = pastPeriodNumberFloor;

            if (pastPeriodNumberCeiling > pastPeriodNumberFloor)
            {
                pastPeriodNumberFixed = pastPeriodNumberCeiling;
            }
            pastPeriodNumberFixed = pastPeriodNumberFixed == 0 ? 1 : pastPeriodNumberFixed;
            return startMilliseconds + pastPeriodNumberFixed * statPeriodMilliseconds - 1;
        }

        /// <summary>
        /// Get the current unified time
        /// </summary>
        /// <returns></returns>
        private DateTimeOffset GetCurrentLocalTime()
        {
            return _timeProvider.GetCurrentLocalTime();
        }

        /// <summary>
        /// Gets the sum of the counts of multiple rate limit targets
        /// </summary>
        /// <param name="targets">The targets</param>
        /// <returns></returns>
        private long Sum(IEnumerable<string> targets)
        {
            var values = _cache.GetValues(targets);
            if (values != null && values.Count > 0)
            {
                Debug.WriteLine(string.Join(",", values.Select(d => ((CountValue)d.Value).Value.ToString())));
                return values.Where(d => d.Value != null).Select(d => ((CountValue)d.Value).Value).Sum();
            }

            return 0;
        }

        private Tuple<bool, long> SimpleIncrement(string target, long amount, DateTimeOffset expireTime, int checkNumber = 0)
        {
            var result = _cache.GetCacheItem(target);
            if (result != null)
            {
                var countValue = result.Value as CountValue;
                if (checkNumber > 0 && countValue.Value >= checkNumber)
                {
                    return Tuple.Create(true, countValue.Value);
                }
                countValue.Value += amount;
                return Tuple.Create(false, countValue.Value);
            }

            _cache.Add(target, new CountValue(amount), expireTime);

            return Tuple.Create(false, amount);
        }

        private long GetOrSetStartTime(string target, TimeSpan statWindow, StartTimeType startTimeType, DateTimeOffset currentTime)
        {
            long startMilliseconds = 0;
            if (!hasSetStartTime)
            {
                var startTime = CalculateStartTime(statWindow, startTimeType, currentTime);
                startMilliseconds = startTime.ToUnixTimeMilliseconds();
                var cachedStartMilliseconds = _cache.AddOrGetExisting($"{target}-st", startMilliseconds, DateTimeOffset.MaxValue);
                if (cachedStartMilliseconds == null) hasSetStartTime = true;
            }
            else
            {
                startMilliseconds = (long)_cache.Get($"{target}-st");
            }

            return startMilliseconds;
        }

        private DateTimeOffset CalculateStartTime(TimeSpan statWindow, StartTimeType startTimeType, DateTimeOffset currentTime)
        {
            DateTimeOffset startTime = currentTime;

            if (startTimeType == StartTimeType.FromNaturalPeriodBeign)
            {
                if (statWindow.Days > 0)
                {
                    startTime = new DateTimeOffset(currentTime.Year, currentTime.Month, currentTime.Day, 0, 0, 0, TimeSpan.FromHours(8));
                }
                else if (statWindow.Hours > 0)
                {
                    startTime = new DateTimeOffset(currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour, 0, 0, TimeSpan.FromHours(8));
                }
                else if (statWindow.Minutes > 0)
                {
                    startTime = new DateTimeOffset(currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour, currentTime.Minute, 0, TimeSpan.FromHours(8));
                }
                else if (statWindow.Seconds > 0)
                {
                    startTime = new DateTimeOffset(currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour, currentTime.Minute, currentTime.Second, TimeSpan.FromHours(8));
                }
            }

            return startTime;
        }

        /// <summary>
        /// Lock the rate limit target until the expiration time, when triggering the rate limit rule.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="expireTimeSpan"></param>
        private void TryLock(string target, TimeSpan expireTimeSpan)
        {
            var expireTime = GetCurrentLocalTime().Add(expireTimeSpan);
            _cache.Add($"{target}-lock", 1, expireTime);
        }

        /// <summary>
        /// Check whether the rate limit target is locked
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private bool CheckLocked(string target)
        {
            return _cache.Get($"{target}-lock") == null ? false : true;
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
