using System;
namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// 限流错误信息
    /// </summary>
    public class RateLimitError
    {
        /// <summary>
        /// 达到阈值及限流锁定时返回的Code，默认429=TooManyRequests
        /// </summary>
        public int Code { get; set; } = 429;

        /// <summary>
        /// 达到阈值及限流锁定时返回的消息内容，默认不返回内容
        /// </summary>
        public string Message { get; set; }
    }
}
