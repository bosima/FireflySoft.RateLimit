using System;
using System.Collections.Generic;

namespace FireflySoft.RateLimit.Core
{
    public interface IRateLimitAlgorithm<TRequest>
    {
        List<RateLimitCheckResult<TRequest>> Check(TRequest Request, IRateLimitStorage storage);
    }
}
