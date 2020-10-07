using System;
namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// 限流规则基类
    /// </summary>
    public abstract class RateLimitRuleBase
    {
        /// <summary>
        /// 限流规则Id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 限流规则名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 统计时间窗口，在这个单位时间内统计访问次数，单位：秒。
        /// </summary>
        public int TimeWindow { get; set; }

        /// <summary>
        /// 统计时间窗口内触发限流的访问计数阈值。
        /// </summary>
        public int LimitNumber { get; set; }

        /// <summary>
        /// 触发限流后锁定的秒数，0代表不锁定
        /// </summary>
        public int LockSeconds { get; set; }
    }
}
