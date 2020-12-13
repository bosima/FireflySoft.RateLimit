using System;
using System.Collections.Generic;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// 滑动窗口限流规则
    /// </summary>
    public class SlidingWindowRateLimitRule<T> : RateLimitRule<T>
    {
        private int _periodNumber;
        
        /// <summary>
        /// 统计时间窗口，在这个单位时间内统计访问次数。
        /// </summary>
        public TimeSpan StatWindow { get; set; }

        /// <summary>
        /// 统计时间窗口内触发限流的访问计数阈值。
        /// </summary>
        public int LimitNumber { get; set; }

        /// <summary>
        /// stat start time
        /// </summary>
        public DateTimeOffset StartTime { get; private set; }

        /// <summary>
        /// stat period
        /// </summary>
        public TimeSpan StatPeriod { get; private set; }

        /// <summary>
        /// create a new instance
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="statWindow"></param>
        /// <param name="statPeriod"></param>
        public SlidingWindowRateLimitRule(DateTimeOffset startTime, TimeSpan statWindow, TimeSpan statPeriod)
        {
            if (statWindow.TotalMilliseconds < 100)
            {
                throw new ArgumentException("the stat window can not less than 100ms.");
            }

            if (statPeriod.TotalMilliseconds < 100)
            {
                throw new ArgumentException("the stat period can not less than 100ms.");
            }

            if (statWindow.Milliseconds % 100 > 0)
            {
                throw new ArgumentException("the number of milliseconds in the stat window must be an integer multiple of 100.");
            }

            if (statPeriod.Milliseconds % 100 > 0)
            {
                throw new ArgumentException("the number of milliseconds in the stat period must be an integer multiple of 100.");
            }

            if (statWindow.TotalMilliseconds % statPeriod.TotalMilliseconds > 0)
            {
                throw new ArgumentException("The stat window must be an integral multiple of the stat period.");
            }

            StartTime = startTime;
            StatWindow = statWindow;
            StatPeriod = statPeriod;

            _periodNumber = (int)(StatWindow.TotalMilliseconds / StatPeriod.TotalMilliseconds);
        }

        public List<string> GetStatWindowPeriodArray()
        {
            var currentPeriod = GetCurrentPeriod();
            var periodSet = new List<string>() { currentPeriod.Ticks.ToString() };
            for (int i = 1; i < _periodNumber; i++)
            {
                periodSet.Add(currentPeriod.AddMilliseconds(0 - StatPeriod.TotalMilliseconds * i).Ticks.ToString());
            }

            return periodSet;
        }

        private DateTimeOffset GetCurrentPeriod()
        {
            var pastTotalTime = DateTimeOffset.Now.Subtract(StartTime).TotalMilliseconds;
            var pastPeriodNumber = pastTotalTime / StatPeriod.TotalMilliseconds;
            var pastPeriodNumberFloor = Math.Floor(pastPeriodNumber);
            var pastPeriodNUmberCeiling = Math.Ceiling(pastPeriodNumber);

            DateTimeOffset currentPeriod;
            if (pastPeriodNUmberCeiling > pastPeriodNumberFloor)
            {
                currentPeriod = StartTime.AddMilliseconds(pastPeriodNUmberCeiling * StatPeriod.TotalMilliseconds - 1);
            }
            else
            {
                if (pastPeriodNumberFloor > 0)
                {
                    currentPeriod = StartTime.AddMilliseconds(pastPeriodNumberFloor * StatPeriod.TotalMilliseconds - 1);
                }
                else
                {
                    currentPeriod = StartTime.AddMilliseconds(StatPeriod.TotalMilliseconds - 1);
                }
            }

            return currentPeriod;
        }
    }
}
