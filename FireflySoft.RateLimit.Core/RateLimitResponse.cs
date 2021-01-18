using System;
namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// Defines the response of rate limit check for any request
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RateLimitResponse<T>
    {
        /// <summary>
        /// If true, it means that the current request should be limited
        /// </summary>
        /// <value></value>
        public bool IsLimit { get; set; }

        /// <summary>
        /// The error when the current request is limited
        /// </summary>
        /// <value></value>
        public RateLimitError Error { get; set; }

        /// <summary>
        /// The rule when the current request is limited
        /// </summary>
        /// <value></value>
        public RateLimitRule<T> Rule { get; set; }
    }
}
