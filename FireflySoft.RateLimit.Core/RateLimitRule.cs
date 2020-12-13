using System;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// 限流规则
    /// </summary>
    public class RateLimitRule<TRequest>
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
        /// 触发限流后锁定的秒数，0代表不锁定
        /// </summary>
        public int LockSeconds { get; set; }

        /// <summary>
        /// 从T的实例中提取限流Target，比如HTTP Header中的某个值。可以返回一个固定值则是对所有用户的访问进行限制。
        /// </summary>
        public Func<TRequest, string> ExtractTarget { get; set; }

        /// <summary>
        /// 检查T的实例是否匹配规则，比如可以检查HttpContext中的路径、HTTP Header等。
        /// </summary>
        public Func<TRequest, bool> CheckRuleMatching { get; set; }
    }
}
