using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Caching;
using System.Threading.Tasks;
using FireflySoft.RateLimit.Core.Rule;
using FireflySoft.RateLimit.Core.Time;

namespace FireflySoft.RateLimit.Core.InProcessAlgorithm
{
    /// <summary>
    /// Define a counter for fixed window algorithm
    /// </summary>
    public class FixedWindowCounter
    {
        /// <summary>
        /// The Count Value
        /// </summary>
        /// <value></value>
        public long Value { get; set; }

        /// <summary>
        /// The start time of current window 
        /// </summary>
        /// <value></value>
        public DateTimeOffset StartTime { get; set; }

        /// <summary>
        /// The statistical time window
        /// </summary>
        /// <value></value>
        public TimeSpan StatWindow { get; set; }
    }
}