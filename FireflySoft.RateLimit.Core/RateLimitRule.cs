using System;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// 限流规则
    /// </summary>
    public class RateLimitRule<T> : RateLimitRuleBase
    {
        /// <summary>
        /// 从T的实例中提取限流Target，比如HTTP Header中的某个值。可以返回一个固定值则是对所有用户的访问进行限制。
        /// </summary>
        public Func<T, string> ExtractTarget{ get; set; }

        /// <summary>
        /// 检查T的实例是否匹配规则，比如可以检查HttpContext中的路径、HTTP Header等。
        /// </summary>
        public Func<T, bool> CheckRuleMatching{ get; set; }
    }
}
