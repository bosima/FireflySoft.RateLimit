using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FireflySoft.RateLimit.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace FireflySoft.RateLimit.AspNetCore
{
    /// <summary>
    /// Defines the http error response for rate limit
    /// </summary>
    public class HttpErrorResponse
    {
        /// <summary>
        /// Create a new instance
        /// </summary>
        public HttpErrorResponse()
        {
        }

        /// <summary>
        /// Get or set the http response status code.
        /// </summary>
        /// <value></value>
        public int HttpStatusCode { get; set; } = 429;

        /// <summary>
        /// A delegates that defines from which response headers are builded.
        /// Asynchronous method is preferred, and this method is used when asynchronous method does not exist.
        /// </summary>
        /// <value></value>
        public Func<HttpContext, AlgorithmCheckResult, Dictionary<string, StringValues>> BuildHttpHeaders { get; set; }

        /// <summary>
        /// A delegates that defines from which response content are builded.
        /// Asynchronous method is preferred, and this method is used when asynchronous method does not exist.
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