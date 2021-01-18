using System;
using System.Collections.Generic;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// Defines a mechanism for access the rate limit data.
    /// </summary>
    public interface IRateLimitStorage
    {
        /// <summary>
        /// Lock the rate limit target until the expiration time, when triggering the rate limit rule.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="expireTimeSpan"></param>
        void TryLock(string target, TimeSpan expireTimeSpan);

        /// <summary>
        /// Check whether the rate limit target is locked
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        bool CheckLocked(string target);

        /// <summary>
        /// Get the current unified time
        /// </summary>
        /// <returns></returns>
        long GetCurrentTime();

        /// <summary>
        /// Adds a rate limit target to the storage by using the specified function if the target does not already exist. Returns the new value, or the existing value if the target exists.
        /// </summary>
        /// <param name="target">The target to add.</param>
        /// <param name="retrieveMethod">The function used to generate a value for the target.</param>
        /// <returns></returns>
        long GetOrAdd(string target, Lazy<long> retrieveMethod);

        /// <summary>
        /// Gets the sum of the counts of multiple rate limit targets
        /// </summary>
        /// <param name="targets">The targets</param>
        /// <returns></returns>
        long Sum(IEnumerable<string> targets);

        /// <summary>
        /// Increase the count value of the rate limit target. When the target does not exist, create it first and increase the specified value, then set its expiration time.
        /// </summary>
        /// <param name="target">The target</param>
        /// <param name="amount">amount of increase</param>
        /// <param name="expireTimeSpan">The expiration time is set when the target is created</param>
        /// <returns>amount of requests</returns>
        long SimpleIncrement(string target, long amount, TimeSpan expireTimeSpan);

        /// <summary>
        /// Increase the count value of the rate limit target for leaky bucket algorithm.
        /// </summary>
        /// <param name="target">The target</param>
        /// <param name="amount">amount of increase</param>
        /// <param name="capacity">The capacity of leaky bucket</param>
        /// <param name="outflowUnit">The time unit of outflow from the leaky bucket</param>
        /// <param name="outflowQuantityPerUnit">The outflow quantity per unit time</param>
        /// <returns>Amount of request in the bucket</returns>
        long LeakyBucketIncrement(string target, long amount, long capacity, int outflowUnit, int outflowQuantityPerUnit);

        /// <summary>
        /// Decrease the count value of the rate limit target for token bucket algorithm.
        /// </summary>
        /// <param name="target">The target</param>
        /// <param name="amount">amount of decrease</param>
        /// <param name="capacity">The capacity of token bucket</param>
        /// <param name="inflowUnit">The time unit of inflow to the bucket bucket</param>
        /// <param name="inflowQuantityPerUnit">The inflow quantity per unit time</param>
        /// <returns>Amount of token in the bucket</returns>
        long TokenBucketDecrement(string target, long amount, long capacity, int inflowUnit, int inflowQuantityPerUnit);
    }
}
