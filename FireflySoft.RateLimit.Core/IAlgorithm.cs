using System.Collections.Generic;
using System.Threading.Tasks;
using FireflySoft.RateLimit.Core.Rule;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    ///  Defines a mechanism for using rate limit algorithm.
    /// </summary>
    public interface IAlgorithm
    {
        /// <summary>
        /// Check the request and return the rate limit result
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        AlgorithmCheckResult Check(object request);

        /// <summary>
        /// Check the request and return the rate limit result
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<AlgorithmCheckResult> CheckAsync(object request);

        /// <summary>
        /// Update the rate limit rules
        /// </summary>
        /// <param name="rules"></param>
        void UpdateRules(IEnumerable<RateLimitRule> rules);

        /// <summary>
        /// Update the rate limit rules
        /// </summary>
        /// <param name="rules"></param>
        Task UpdateRulesAsync(IEnumerable<RateLimitRule> rules);

        /// <summary>
        /// Peek at the rate limit check results at the current time.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        AlgorithmCheckResult Peek(string target);

        /// <summary>
        /// Peek at the rate limit check results at the current time.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        Task<AlgorithmCheckResult> PeekAsync(string target);
    }
}