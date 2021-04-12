using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FireflySoft.RateLimit.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace FireflySoft.RateLimit.AspNetCore
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
        public Func<HttpContext, AlgorithmCheckResult, Dictionary<string, StringValues>> BuildHttpHeaders { get; set; }

        /// <summary>
        /// A delegates that defines from which response content are builded.
        /// </summary>
        /// <value></value>
        public Func<HttpContext, AlgorithmCheckResult, string> BuildHttpContent { get; set; }

        /// <summary>
        /// A delegates that defines from which response headers are builded.
        /// </summary>
        /// <value></value>
        public Func<HttpContext, AlgorithmCheckResult, Task<Dictionary<string, StringValues>>> BuildHttpHeadersAsync { get; set; }

        /// <summary>
        /// A delegates that defines from which response content are builded.
        /// </summary>
        /// <value></value>
        public Func<HttpContext, AlgorithmCheckResult, Task<string>> BuildHttpContentAsync { get; set; }
    }
}