using System;
using System.Threading.Tasks;
using FireflySoft.RateLimit.Core;
using Microsoft.AspNetCore.Http;

namespace FireflySoft.RateLimit.AspNetCore
{
    /// <summary>
    /// Http Invoke Interceptor
    /// </summary>
    public class HttpInvokeInterceptor
    {
        /// <summary>
        /// Do work before rate limit check
        /// </summary>
        /// <value></value>
        public Action<HttpContext, IAlgorithm> OnBeforeCheck { get; set; }

        /// <summary>
        /// Do work before rate limit check
        /// </summary>
        /// <value></value>
        public Func<HttpContext, IAlgorithm, Task> OnBeforeCheckAsync { get; set; }

        /// <summary>
        /// Do work after rate limit check
        /// </summary>
        /// <value></value>
        public Action<HttpContext, AlgorithmCheckResult> OnAfterCheck { get; set; }

        /// <summary>
        /// Do work after rate limit check
        /// </summary>
        /// <value></value>
        public Func<HttpContext, AlgorithmCheckResult, Task> OnAfterCheckAsync { get; set; }

        /// <summary>
        /// Do work when rate limit triggered
        /// </summary>
        /// <value></value>
        public Action<HttpContext, AlgorithmCheckResult> OnTriggered { get; set; }

        /// <summary>
        /// Do work when rate limit triggered
        /// </summary>
        /// <value></value>
        public Func<HttpContext, AlgorithmCheckResult, Task> OnTriggeredAsync { get; set; }

        /// <summary>
        /// Do work when rate limit not triggered and before do next middleware.
        /// Doesn't write to the Response.
        /// </summary>
        /// <value></value>
        public Action<HttpContext, AlgorithmCheckResult> OnBreforUntriggeredDoNext { get; set; }

        /// <summary>
        /// Do work when rate limit not triggered and before do next middleware.
        /// Doesn't write to the Response.
        /// </summary>
        /// <value></value>
        public Func<HttpContext, AlgorithmCheckResult, Task> OnBreforUntriggeredDoNextAsync { get; set; }

        /// <summary>
        /// Do work when rate limit not triggered and after do next middleware.
        /// Doesn't write to the Response.
        /// </summary>
        /// <value></value>
        public Action<HttpContext, AlgorithmCheckResult> OnAfterUntriggeredDoNext { get; set; }

        /// <summary>
        /// Do work when rate limit not triggered and after do next middleware.
        /// Doesn't write to the Response.
        /// </summary>
        /// <value></value>
        public Func<HttpContext, AlgorithmCheckResult, Task> OnAfterUntriggeredDoNextAsync { get; set; }
    }
}