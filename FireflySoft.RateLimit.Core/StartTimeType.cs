using System;
namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// The type of statistics start time
    /// </summary>
    public enum StartTimeType
    {
        /// <summary>
        /// From the current time.
        /// </summary>
        FromCurrent = 1,

        /// <summary>
        /// From the beginning of the natural period.
        /// In this way, the statistical time window must be an integer and coincide with the natural time period.
        /// </summary>
        FromNaturalPeriodBeign = 2
    }
}
