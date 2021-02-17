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
        /// <param name="statWindow">The expiration time is set when the target is created</param>
        /// <param name="limitNumber">The number of rate limit</param>
        /// <param name="lockSeconds">The number of seconds locked after triggering rate limiting. 0 means not locked</param>
        /// <returns>amount of requests</returns>
        public Tuple<bool, long> FixedWindowIncrement(string target, long amount, TimeSpan statWindow, int limitNumber, int lockSeconds)
        {
            bool locked = CheckLocked(target);
            if (locked)
            {
                return new Tuple<bool, long>(true, -1);
            }

            Tuple<bool, long> incrementResult;
            lock (target)
            {
                incrementResult = SimpleIncrement(target, amount, statWindow, limitNumber);
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
        /// <param name="statWindow">The expiration time is set when the target is created</param>
        /// <param name="limitNumber">The number of rate limit</param>
        /// <param name="lockSeconds">The number of seconds locked after triggering rate limiting. 0 means not locked</param>
        /// <returns>amount of requests</returns>
        public async Task<Tuple<bool, long>> FixedWindowIncrementAsync(string target, long amount, TimeSpan statWindow, int limitNumber, int lockSeconds)
        {
            return await Task.FromResult(FixedWindowIncrement(target, amount, statWindow, limitNumber, lockSeconds));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="amount"></param>
        /// <param name="statWindow"></param>
        /// <param name="statPeriod"></param>
        /// <param name="periodNumber"></param>
        /// <param name="limitNumber">The number of rate limit</param>
        /// <param name="lockSeconds">The number of seconds locked after triggering rate limiting. 0 means not locked</param>
        /// <returns></returns>
        public Tuple<bool, long> SlidingWindowIncrement(string target, long amount, TimeSpan statWindow, TimeSpan statPeriod, int periodNumber, int limitNumber, int lockSeconds)
        {
            bool locked = CheckLocked(target);
            if (locked)
            {
                return new Tuple<bool, long>(true, -1);
            }

            // get current time
            var currentMilliseconds = GetCurrentTime();

            // get the start time
            long startMilliseconds = currentMilliseconds;
            var cachedStartMilliseconds = _cache.AddOrGetExisting($"{target}-st", currentMilliseconds, DateTimeOffset.MaxValue);
            if (cachedStartMilliseconds != null)
            {
                startMilliseconds = (long)cachedStartMilliseconds;
            }

            // get the stat periods
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
                var expireTimeSpan = statWindow.Add(statWindow); // for the calculating time
                var incrementKey = $"{target}-{currentPeriod}";
                SimpleIncrement(incrementKey, amount, expireTimeSpan);

                return Tuple.Create(false, totalAmount);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="amount"></param>
        /// <param name="statWindow"></param>
        /// <param name="statPeriod"></param>
        /// <param name="periodNumber"></param>
        /// <param name="limitNumber">The number of rate limit</param>
        /// <param name="lockSeconds">The number of seconds locked after triggering rate limiting. 0 means not locked</param>
        /// <returns></returns>
        public async Task<Tuple<bool, long>> SlidingWindowIncrementAsync(string target, long amount, TimeSpan statWindow, TimeSpan statPeriod, int periodNumber, int limitNumber, int lockSeconds)
        {
            return await Task.FromResult(SlidingWindowIncrement(target, amount, statWindow, statPeriod, periodNumber, limitNumber, lockSeconds));
        }

        /// <summary>
        /// Increase the count value of the rate limit target for leaky bucket algorithm.
        /// </summary>
        /// <param name="target">The target</param>
        /// <param name="amount">amount of increase</param>
        /// <param name="capacity">The capacity of leaky bucket</param>
        /// <param name="outflowUnit">The time unit of outflow from the leaky bucket</param>
        /// <param name="outflowQuantityPerUnit">The outflow quantity per unit time</param>
        /// <param name="lockSeconds">The number of seconds locked after triggering rate limiting. 0 means not locked</param>
        /// <returns>Amount of request in the bucket</returns>
        public Tuple<bool, long> LeakyBucketIncrement(string target, long amount, long capacity, int outflowUnit, int outflowQuantityPerUnit, int lockSeconds)
        {
            bool locked = CheckLocked(target);
            if (locked)
            {
                return new Tuple<bool, long>(true, -1);
            }

            lock (target)
            {
                var now = DateTimeOffset.Now;

                var result = _cache.GetCacheItem(target);
                if (result == null)
                {
                    _cache.Add(target, new CountValue(amount) { LastFlowTime = now }, DateTimeOffset.MaxValue);
                    return new Tuple<bool, long>(false, amount);
                }

                var countValue = (CountValue)result.Value;
                var lastTime = countValue.LastFlowTime;
                var pastTime = now - lastTime;
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
        /// <param name="lockSeconds">The number of seconds locked after triggering rate limiting. 0 means not locked</param>
        /// <returns>Amount of request in the bucket</returns>
        public async Task<Tuple<bool, long>> LeakyBucketIncrementAsync(string target, long amount, long capacity, int outflowUnit, int outflowQuantityPerUnit, int lockSeconds)
        {
            return await Task.FromResult(LeakyBucketIncrement(target, amount, capacity, outflowUnit, outflowQuantityPerUnit, lockSeconds));
        }

        /// <summary>
        /// Decrease the count value of the rate limit target for token bucket algorithm.
        /// </summary>
        /// <param name="target">The target</param>
        /// <param name="amount">amount of decrease</param>
        /// <param name="capacity">The capacity of token bucket</param>
        /// <param name="inflowUnit">The time unit of inflow to the bucket bucket</param>
        /// <param name="inflowQuantityPerUnit">The inflow quantity per unit time</param>
        /// <param name="lockSeconds">The number of seconds locked after triggering rate limiting. 0 means not locked</param>
        /// <returns>Amount of token in the bucket</returns>
        public Tuple<bool, long> TokenBucketDecrement(string target, long amount, long capacity, int inflowUnit, int inflowQuantityPerUnit, int lockSeconds)
        {
            bool locked = CheckLocked(target);
            if (locked)
            {
                return new Tuple<bool, long>(true, -1);
            }

            lock (target)
            {
                var now = DateTimeOffset.Now;
                long bucketAmount = 0;
                var result = _cache.GetCacheItem(target);
                if (result == null)
                {
                    bucketAmount = capacity - amount;
                    _cache.Add(target, new CountValue(bucketAmount) { LastFlowTime = now }, DateTimeOffset.MaxValue);
                    return new Tuple<bool, long>(false, bucketAmount);
                }

                var countValue = (CountValue)result.Value;
                var lastTime = countValue.LastFlowTime;
                var lastTimeChanged = false;
                var pastTime = now - lastTime;
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
        /// <param name="lockSeconds">The number of seconds locked after triggering rate limiting. 0 means not locked</param>
        /// <returns>Amount of token in the bucket</returns>
        public async Task<Tuple<bool, long>> TokenBucketDecrementAsync(string target, long amount, long capacity, int inflowUnit, int inflowQuantityPerUnit, int lockSeconds)
        {
            return await Task.FromResult(TokenBucketDecrement(target, amount, capacity, inflowUnit, inflowQuantityPerUnit, lockSeconds));
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
        private long GetCurrentTime()
        {
            return DateTimeOffset.Now.ToUnixTimeMilliseconds();
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

        private Tuple<bool, long> SimpleIncrement(string target, long amount, TimeSpan expireTimeSpan, int checkNumber = 0)
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
            return Tuple.Create(false, amount);
        }

        /// <summary>
        /// Lock the rate limit target until the expiration time, when triggering the rate limit rule.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="expireTimeSpan"></param>
        private void TryLock(string target, TimeSpan expireTimeSpan)
        {
            var expireTime = DateTimeOffset.Now.Add(expireTimeSpan);
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
