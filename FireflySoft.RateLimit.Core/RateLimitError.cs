using System;
namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// Defines the error message of rate limit
    /// </summary>
    public class RateLimitError
    {
        /// <summary>
        /// The error code returned when the request is limited. The default value is 429 = TooManyRequests
        /// </summary>
        public int Code { get; set; } = 429;

        /// <summary>
        /// The error message returned when the request is limited.
        /// </summary>
        public string Message { get; set; }
    }
}
