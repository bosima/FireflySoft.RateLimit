using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// Algorithm Check Result
    /// </summary>
    public class AlgorithmCheckResult
    {
        private IEnumerable<RuleCheckResult> _ruleCheckResults;

        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="ruleCheckResults"></param>
        public AlgorithmCheckResult(IEnumerable<RuleCheckResult> ruleCheckResults)
        {
            _ruleCheckResults = ruleCheckResults;
        }

        /// <summary>
        /// If true, it means that the current request should be limited
        /// </summary>
        /// <value></value>
        public bool IsLimit
        {
            get
            {
                return _ruleCheckResults.Any(d => d.IsLimit);
            }
        }

        /// <summary>
        /// The rule check results.
        /// </summary>
        /// <value></value>
        public IEnumerable<RuleCheckResult> RuleCheckResults
        {
            get
            {
                return _ruleCheckResults;
            }
        }
    }
}