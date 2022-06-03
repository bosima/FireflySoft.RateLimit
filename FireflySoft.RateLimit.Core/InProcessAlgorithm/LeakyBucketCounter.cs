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
    /// Define a counter for leaky bucket algorithm
    /// </summary>
    public class LeakyBucketCounter
    {
        /// <summary>
        /// The number of requests that allowed to be processed in the current time window,
        /// including the requests in the leaky bucket and the requests that have flowed out in the current time window.
        /// </summary>
        /// <value></value>
        public long Value { get; set; }

        /// <summary>
        /// The last flow-out time
        /// </summary>
        /// <value></value>
        public DateTimeOffset LastFlowOutTime { get; set; }
    }
}