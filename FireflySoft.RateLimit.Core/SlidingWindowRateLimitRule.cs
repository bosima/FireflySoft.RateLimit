using System;
using System.Collections.Generic;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// The rule of sliding window algorithm
    /// </summary>
    public class SlidingWindowRateLimitRule<T> : RateLimitRule<T>
    {
        private int _periodNumber;

        /// <summary>
        /// Statistical time window, which counts the number of requests in this time.
        /// </summary>
        public TimeSpan StatWindow { get; set; }

        /// <summary>
        /// The threshold of triggering rate limit in the statistical time window。
        /// </summary>
        public int LimitNumber { get; set; }

        /// <summary>
        /// Small period length in statistical time window
        /// </summary>
        public TimeSpan StatPeriod { get; private set; }

        /// <summary>
        /// create a new instance
        /// </summary>
        /// <param name="statWindow"></param>
        /// <param name="statPeriod"></param>
        public SlidingWindowRateLimitRule(TimeSpan statWindow, TimeSpan statPeriod)
        {
            if (statWindow.TotalMilliseconds < 1)
            {
                throw new ArgumentException("the stat window can not less than 1ms.");
            }

            if (statPeriod.TotalMilliseconds < 1)
            {
                throw new ArgumentException("the stat period can not less than 1ms.");
            }

            if (statWindow.TotalMilliseconds % statPeriod.TotalMilliseconds > 0)
            {
                throw new ArgumentException("The stat window must be an integral multiple of the stat period.");
            }

            StatWindow = statWindow;
            StatPeriod = statPeriod;

            _periodNumber = (int)(StatWindow.TotalMilliseconds / StatPeriod.TotalMilliseconds);
        }

        /// <summary>
        /// Gets all small periods in statistical time window
        /// </summary>
        /// <param name="startTime"></param>
        /// <returns></returns>
        public List<string> GetStatWindowPeriodArray(long startTime)
        {
            var currentPeriod = GetCurrentPeriod(startTime);
            var periodSet = new List<string>() { currentPeriod.ToUnixTimeMilliseconds().ToString() };
            for (int i = 1; i < _periodNumber; i++)
            {
                periodSet.Add(currentPeriod.AddMilliseconds(0 - StatPeriod.TotalMilliseconds * i).ToUnixTimeMilliseconds().ToString());
            }

            return periodSet;
        }

        private DateTimeOffset GetCurrentPeriod(long startTime)
        {
            var pastTotalTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() - startTime;
            var pastPeriodNumber = pastTotalTime / StatPeriod.TotalMilliseconds;
            var pastPeriodNumberFloor = (long)Math.Floor(pastPeriodNumber);
            var pastPeriodNUmberCeiling = (long)Math.Ceiling(pastPeriodNumber);

            DateTimeOffset currentPeriod;
            if (pastPeriodNUmberCeiling > pastPeriodNumberFloor)
            {
                currentPeriod = DateTimeOffset.FromUnixTimeMilliseconds(startTime + pastPeriodNUmberCeiling * (long)StatPeriod.TotalMilliseconds - 1);
            }
            else
            {
                if (pastPeriodNumberFloor > 0)
                {
                    currentPeriod = DateTimeOffset.FromUnixTimeMilliseconds(startTime + pastPeriodNumberFloor * (long)StatPeriod.TotalMilliseconds - 1);
                }
                else
                {
                    currentPeriod = DateTimeOffset.FromUnixTimeMilliseconds(startTime + (long)StatPeriod.TotalMilliseconds - 1);
                }
            }

            return currentPeriod;
        }
    }
}
