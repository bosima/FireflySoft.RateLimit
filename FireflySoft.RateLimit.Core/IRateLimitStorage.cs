using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// Defines a mechanism for access the rate limit data.
    /// </summary>
    public interface IRateLimitStorage
    {
        /// <summary>
        /// Increase the count value of the rate limit target. When the target does not exist, create it first and increase the specified value, then set its expiration time.
        /// </summary>
        /// <param name="target">The target</param>
        /// <param name="amount">Amount of per increase</param>
        /// <param name="expireTimeSpan">The expiration time is set when the target is created</param>
        /// <param name="limitNumber">The number of rate limit</param>
        /// <param name="lockSeconds">The number of seconds locked after triggering rate limiting. 0 means not locked</param>
        /// <returns>amount of requests</returns>
        Tuple<bool, long> FixedWindowIncrement(string target, long amount, TimeSpan expireTimeSpan, int limitNumber, int lockSeconds);

        /// <summary>
        /// Increase the count value of the rate limit target. When the target does not exist, create it first and increase the specified value, then set its expiration time.
        /// </summary>
        /// <param name="target">The target</param>
        /// <param name="amount">amount of increase</param>
        /// <param name="expireTimeSpan">The expiration time is set when the target is created</param>
        /// <param name="limitNumber">The number of rate limit</param>
        /// <param name="lockSeconds">The number of seconds locked after triggering rate limiting. 0 means not locked</param>
        /// <returns>amount of requests</returns>
        Task<Tuple<bool, long>> FixedWindowIncrementAsync(string target, long amount, TimeSpan expireTimeSpan, int limitNumber, int lockSeconds);

        /// <summary>
        /// Increase the count value of the rate limit target for sliding window algorithm.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="amount"></param>
        /// <param name="statWindow"></param>
        /// <param name="statPeriod"></param>
        /// <param name="periodNumber"></param>
        /// <param name="limitNumber">The number of rate limit</param>
        /// <param name="lockSeconds">The number of seconds locked after triggering rate limiting. 0 means not locked</param>
        /// <returns></returns>
        Tuple<bool, long> SlidingWindowIncrement(string target, long amount, TimeSpan statWindow, TimeSpan statPeriod, int periodNumber, int limitNumber, int lockSeconds);

        /// <summary>
        /// Increase the count value of the rate limit target for sliding window algorithm.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="amount"></param>
        /// <param name="statWindow"></param>
        /// <param name="statPeriod"></param>
        /// <param name="periodNumber"></param>
        /// <param name="limitNumber">The number of rate limit</param>
        /// <param name="lockSeconds">The number of seconds locked after triggering rate limiting. 0 means not locked</param>
        /// <returns></returns>
        Task<Tuple<bool, long>> SlidingWindowIncrementAsync(string target, long amount, TimeSpan statWindow, TimeSpan statPeriod, int periodNumber, int limitNumber, int lockSeconds);

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
        Tuple<bool, long> LeakyBucketIncrement(string target, long amount, long capacity, int outflowUnit, int outflowQuantityPerUnit, int lockSeconds);

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
        Task<Tuple<bool, long>> LeakyBucketIncrementAsync(string target, long amount, long capacity, int outflowUnit, int outflowQuantityPerUnit, int lockSeconds);

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
        Tuple<bool, long> TokenBucketDecrement(string target, long amount, long capacity, int inflowUnit, int inflowQuantityPerUnit, int lockSeconds);

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
        Task<Tuple<bool, long>> TokenBucketDecrementAsync(string target, long amount, long capacity, int inflowUnit, int inflowQuantityPerUnit, int lockSeconds);
    }
}
