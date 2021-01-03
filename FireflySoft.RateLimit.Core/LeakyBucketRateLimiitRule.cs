using System;
using System.Collections.Generic;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// 漏斗限流规则
    /// </summary>
    public class LeakyBucketRateLimitRule<T> : RateLimitRule<T>
    {
        /// <summary>
        /// 漏斗的容量。
        /// </summary>
        public int Capacity { get; private set; }

        /// <summary>
        /// 单位时间内的流出量
        /// </summary>
        public int OutflowQuantityPerUnit { get; private set; }

        /// <summary>
        /// 计算流出量的单位时间
        /// </summary>
        public TimeSpan OutflowUnit { get; private set; }

        /// <summary>
        /// create a new instance
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="statWindow"></param>
        /// <param name="statPeriod"></param>
        public LeakyBucketRateLimitRule(int capacity, int outflowQuantityPerUnit, TimeSpan outflowUnit)
        {
            if (capacity < 1)
            {
                throw new ArgumentException("the capacity can not less than 1.");
            }

            if (outflowQuantityPerUnit < 1)
            {
                throw new ArgumentException("the outflow quantity per unit can not less than 1.");
            }

            if (outflowUnit.TotalMilliseconds < 1)
            {
                throw new ArgumentException("the outflow unit can not less than 1ms.");
            }

            Capacity = capacity;
            OutflowQuantityPerUnit = outflowQuantityPerUnit;
            OutflowUnit = outflowUnit;
        }
    }
}
