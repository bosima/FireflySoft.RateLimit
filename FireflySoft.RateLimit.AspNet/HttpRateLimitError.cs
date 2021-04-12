using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using FireflySoft.RateLimit.Core;

namespace FireflySoft.RateLimit.AspNet
{
    /// <summary>
    /// Rate Limit Error
    /// </summary>
    public class HttpRateLimitError
    {
        /// <summary>
        /// Create a new instance
        /// </summary>
        public HttpRateLimitError()
        {
        }

        /// <summary>
        /// Get or set the http response status code.
        /// </summary>
        /// <value></value>
        public int HttpStatusCode { get; set; } = 429;

        /// <summary>
        /// A delegates that defines from which response headers are builded.
        /// </summary>
        /// <value></value>
        public Func<HttpRequestMessage, AlgorithmCheckResult, Dictionary<string, string>> BuildHttpHeaders { get; set; }

        /// <summary>
        /// A delegates that defines from which response content are builded.
        /// </summary>
        /// <value></value>
        public Func<HttpRequestMessage, AlgorithmCheckResult, string> BuildHttpContent { get; set; }

        /// <summary>
        /// A delegates that defines from which response headers are builded.
        /// </summary>
        /// <value></value>
        public Func<HttpRequestMessage, AlgorithmCheckResult, Task<Dictionary<string, string>>> BuildHttpHeadersAsync { get; set; }

        /// <summary>
        /// A delegates that defines from which response content are builded.
        /// </summary>
        /// <value></value>
        public Func<HttpRequestMessage, AlgorithmCheckResult, Task<string>> BuildHttpContentAsync { get; set; }
    }
}