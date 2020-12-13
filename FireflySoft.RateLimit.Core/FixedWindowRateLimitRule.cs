using System;
using System.Collections.Generic;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// 固定窗口限流规则
    /// </summary>
    public class FixedWindowRateLimitRule<T> : RateLimitRule<T>
    {
        /// <summary>
        /// 统计时间窗口，在这个单位时间内统计访问次数。
        /// </summary>
        public TimeSpan StatWindow { get; set; }

        /// <summary>
        /// 统计时间窗口内触发限流的访问计数阈值。
        /// </summary>
        public int LimitNumber { get; set; }
    }
}