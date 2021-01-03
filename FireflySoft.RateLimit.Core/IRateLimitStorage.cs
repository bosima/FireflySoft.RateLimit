using System;
using System.Collections.Generic;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// 限流存储接口
    /// </summary>
    public interface IRateLimitStorage
    {
        /// <summary>
        /// 触发限流条件后锁定目标，直到设置的过期时间
        /// </summary>
        /// <param name="target"></param>
        /// <param name="expireTimeSpan"></param>
        void TryLock(string target, TimeSpan expireTimeSpan);

        /// <summary>
        /// 检查是否限流锁定中
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        bool CheckLocked(string target);

        /// <summary>
        /// 获取当前时间
        /// </summary>
        /// <returns></returns>
        long GetCurrentTime();

        /// <summary>
        /// 数据不存在时添加
        /// </summary>
        /// <param name="target"></param>
        /// <param name="retrieveMethod"></param>
        /// <returns></returns>
        long GetOrAdd(string target,Lazy<long> retrieveMethod);

        /// <summary>
        /// 获取多个目标对应的数值
        /// </summary>
        /// <param name="targets"></param>
        /// <returns></returns>
        long MGet(IEnumerable<string> targets);

        /// <summary>
        /// 增加限流目标的统计值，限流目标不存在时先创建再增加指定的值，并可在首次创建时设置过期时间
        /// </summary>
        /// <param name="target">限流目标</param>
        /// <param name="amount">增加的数量</param>
        /// <param name="expireTimeSpan">首次创建限流目标时设置的过期时间，设置为0时没有过期时间</param>
        /// <returns></returns>
        long Increment(string target, long amount, TimeSpan expireTimeSpan);

        /// <summary>
        /// 漏桶值增加
        /// </summary>
        /// <param name="target">限流目标</param>
        /// <param name="amount">增加的数量</param>
        /// <param name="capacity">漏桶容量</param>
        /// <param name="outflowUnit">漏出时间单位</param>
        /// <param name="outflowQuantityPerUnit">单位时间漏出量</param>
        /// <returns></returns>
        long LeakyBucketIncrement(string target, long amount, long capacity, int outflowUnit, int outflowQuantityPerUnit);

        /// <summary>
        /// 令牌值减少
        /// </summary>
        /// <param name="target">限流目标</param>
        /// <param name="amount">扣减的数量</param>
        /// <param name="capacity">桶容量</param>
        /// <param name="inflowUnit">流入时间单位</param>
        /// <param name="inflowQuantityPerUnit">单位时间流入量</param>
        /// <returns></returns>
        long TokenBucketDecrement(string target, long amount, long capacity, int inflowUnit, int inflowQuantityPerUnit);
    }
}
