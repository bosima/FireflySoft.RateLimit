Github: https://github.com/bosima/FireflySoft.RateLimit

## Introduction
Fireflysoft.RateLimit is a rate limiting library based on .Net standard. Its core is simple and lightweight, and can flexibly meet the rate limiting needs of many scenarios.

## Features
* Multiple rate limiting algorithms: built-in fixed window, sliding window, leaky bucket, token bucket, and can be extended.
* Multiple counting storage: memory and Redis (including cluster).
* Distributed friendly: supports unified counting of distributed programs with Redis storage.
* Flexible rate limiting targets: each data can be extracted from the request to set rate limiting targets.
* Support rate limit penalty: the client can be locked for a period of time after the rate limit is triggered.
* Time window enhancement: support to the millisecond level; support starting from the starting point of time periods such as seconds, minutes, hours, dates, etc.
* Real-time tracking: the number of requests processed and the remaining allowed requests in the current counting cycle, as well as the reset time of the counting cycle.
* Dynamically change the rules: support the dynamic change of the rate limiting rules when the program is running.
* Custom error: you can customize the error code and error message after the current limit is triggered.
* Universality: in principle, it can meet any scenario that requires rate limiting.

## Usage

**If you need to use it in ASP.NET Core, it is recommended to install the package FireflySoft.RateLimit.AspNetCore.**

Use *IAlgorithm* to filter every request, process the return value of *Check* method.

```csharp
// Rule
var fixedWindowRules = new FixedWindowRule[]
    {
        new FixedWindowRule()
        {
            Id = "3",
            StatWindow=TimeSpan.FromSeconds(1),
            LimitNumber=30,
            ExtractTarget = (request) =>
            {
                return (request as SimulationRequest).RequestResource;
            },
            CheckRuleMatching = (request) =>
            {
                return true;
            },
        }
    };

// Algorithm
IAlgorithm algorithm = new InProcessFixedWindowAlgorithm(fixedWindowRules);

// Check
var result = algorithm.Check(new SimulationRequest()
    {
        RequestId = Guid.NewGuid().ToString(),
        RequestResource = "home",
        Parameters = new Dictionary<string, string>() {
                    { "from","sample" },
            }
    });
```

SimulationRequest is a custom request that you can modify to any type.