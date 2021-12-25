using System;

namespace FireflySoft.RateLimit.Core.InProcessAlgorithm
{
    /// <summary>
    /// Define a counter for token bucket algorithm
    /// </summary>
    public class TokenBucketCounter
    {
        /// <summary>
        /// The Count Value
        /// </summary>
        /// <value></value>
        public long Value { get; set; }

        /// <summary>
        /// The last flow-in time
        /// </summary>
        /// <value></value>
        public DateTimeOffset LastFlowInTime { get; set; }
    }
}