using System;
using System.Collections.Generic;

namespace FireflySoft.RateLimit.Core.Rule
{
    /// <summary>
    /// The rule of sliding window algorithm
    /// </summary>
    public class SlidingWindowRule : RateLimitRule
    {
        private int _periodNumber;

        /// <summary>
        /// Gets the amount of small periods.
        /// </summary>
        /// <value></value>
        public int PeriodNumber
        {
            get
            {
                return _periodNumber;
            }
        }

        /// <summary>
        /// Gets or sets the statistical time window, which counts the amount of requests in this time.
        /// </summary>
        public TimeSpan StatWindow { get; set; }

        /// <summary>
        /// Gets or sets the threshold of triggering rate limiting in the statistical time window.
        /// </summary>
        public int LimitNumber { get; set; }

        /// <summary>
        /// Gets the small period length in statistical time window.
        /// </summary>
        public TimeSpan StatPeriod { get; private set; }

        /// <summary>
        /// Create a new instance of SlidingWindowRule
        /// </summary>
        /// <param name="statWindow"></param>
        /// <param name="statPeriod"></param>
        public SlidingWindowRule(TimeSpan statWindow, TimeSpan statPeriod)
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
    }
}
