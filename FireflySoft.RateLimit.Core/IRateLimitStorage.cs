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
        void Lock(string target, TimeSpan expireTimeSpan);

        /// <summary>
        /// 检查是否限流锁定中
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        bool CheckLocking(string target);

        /// <summary>
        /// KV操作：如果不存在则设置
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns>设置成功，返回true，否则返回false</returns>
        //bool SetIfNoExsit(string key, string value);

        /// <summary>
        /// 获取目标对应的数值
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        long Get(string target);

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
        /// <param name="maxAmount">限流目标的最大值，如果增加后的数量将大于或等于此值，则设置为此值。如果为0，则不做此判断。/param>
        /// <param name="checkTarget">检查目标，如果检查目标的实际值与预期值匹配，则进行设置。如果为空，则不进行设置。</param>
        /// <param name="checkExpectValue">检查目标的预期值，如果设置了检查目标，则使用此参数</param>
        /// <param name="checkNewValue">检查目标的新值，数量设置成功后将同时更新此值。</param>
        /// <returns></returns>
        long Increment(string target, long amount, TimeSpan expireTimeSpan);
        //long Increment(string target, long amount, TimeSpan expireTimeSpan, long maxAmount, string checkTarget, string checkExpectValue, string checkNewValue);

        /// <summary>
        /// 减少限流目标的统计值
        /// </summary>
        /// <param name="target"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        //long Decrement(string target, long amount);
    }
}
