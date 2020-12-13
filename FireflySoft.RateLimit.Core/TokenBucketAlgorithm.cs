using System;
using System.Collections.Generic;

namespace FireflySoft.RateLimit.Core
{
    public class TokenBucketAlgorithm<TRequest> : IRateLimitAlgorithm<TRequest>
    {
        public TokenBucketAlgorithm()
        {
        }

        public List<RateLimitCheckResult<TRequest>> Check(TRequest request, IRateLimitStorage storage)
        {
            throw new NotImplementedException();
        }
    }
}
