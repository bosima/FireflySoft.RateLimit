using System;
using System.Runtime.Caching;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// 固定窗口算法
    /// </summary>
    public class FixedWindowAlgorithm : IRateLimitAlgorithm
    {
        public FixedWindowAlgorithm()
        {
        }

        public bool Check(string target, IRateLimitStorage storage, RateLimitRuleBase rule)
        {
            if (storage.CheckIsLocked(target))
            {
                return true;
            }

            var countAmount = 1;
            var expireTimeSpan = TimeSpan.FromSeconds(rule.TimeWindow);
            var totalAmount = storage.Increment(target, countAmount, expireTimeSpan);
            if (totalAmount > rule.LimitNumber)
            {
                if (rule.LockSeconds > 0)
                {
                    storage.Lock(target, TimeSpan.FromSeconds(rule.LockSeconds));
                }

                return true;
            }

            return false;
        }
    }
}

