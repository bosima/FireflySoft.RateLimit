using System;
namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// 限流存储接口
    /// </summary>
    public interface IRateLimitStorage
    {
        /// <summary>
        /// 锁定目标，直到设置的过期时间
        /// </summary>
        /// <param name="target"></param>
        /// <param name="expireTimeSpan"></param>
        void Lock(string target, TimeSpan expireTimeSpan);

        /// <summary>
        /// 检查是否被锁定
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        bool CheckIsLocked(string target);

        /// <summary>
        /// 增加限流目标的统计值，并在首次创建时设置过期时间
        /// </summary>
        /// <param name="target"></param>
        /// <param name="amount"></param>
        /// <param name="expireTimeSpan"></param>
        /// <returns></returns>
        long Increment(string target, long amount, TimeSpan expireTimeSpan);
    }
}
