using System;
using System.Collections.Generic;

namespace FireflySoft.RateLimit.Core
{
    public class LeakyBucketAlgorithm<TRequest> : IRateLimitAlgorithm<TRequest>
    {
        public LeakyBucketAlgorithm()
        {
        }

        public List<RateLimitCheckResult<TRequest>> Check(TRequest request, IRateLimitStorage storage)
        {
            throw new NotImplementedException();
        }
    }
}
