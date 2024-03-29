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

The following code calls the rate-limit [middleware](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-3.1) from Startup.Configure:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    ...

    services.AddRateLimit(new InProcessFixedWindowAlgorithm(
        new[] {
            new FixedWindowRule()
            {
                ExtractTarget = context =>
                {
                    return (context as HttpContext).Request.Path.Value;
                },
                CheckRuleMatching = context =>
                {
                    return true;
                },
                Name="default limit rule",
                LimitNumber=30,
                StatWindow=TimeSpan.FromSeconds(1)
            }
        })
    );

    ...
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    ...

    app.UseRateLimit();

    ...
}
```