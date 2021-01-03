using System;
using System.Collections.Generic;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// 令牌桶限流规则
    /// </summary>
    public class TokenBucketRateLimitRule<T> : RateLimitRule<T>
    {
        /// <summary>
        /// 令牌桶的容量。
        /// </summary>
        public int Capacity { get; private set; }

        /// <summary>
        /// 单位时间内的流入量
        /// </summary>
        public int InflowQuantityPerUnit { get; private set; }

        /// <summary>
        /// 计算流入量的单位时间
        /// </summary>
        public TimeSpan InflowUnit { get; private set; }

        /// <summary>
        /// create a new instance
        /// </summary>
        /// <param name="capacity"></param>
        /// <param name="inflowQuantityPerUnit"></param>
        /// <param name="inflowUnit"></param>
        public TokenBucketRateLimitRule(int capacity, int inflowQuantityPerUnit, TimeSpan inflowUnit)
        {
            if (capacity < 1)
            {
                throw new ArgumentException("the capacity can not less than 1.");
            }

            if (inflowQuantityPerUnit < 1)
            {
                throw new ArgumentException("the inflow quantity per unit can not less than 1.");
            }

            if (inflowUnit.TotalMilliseconds < 1)
            {
                throw new ArgumentException("the inflow unit can not less than 1ms.");
            }

            Capacity = capacity;
            InflowQuantityPerUnit = inflowQuantityPerUnit;
            InflowUnit = inflowUnit;
        }
    }
}
