# FireflySoft.RateLimit　　　　　　　　　　　[中文](https://github.com/bosima/FireflySoft.RateLimit/blob/master/README.zh-CN.md)

- [FireflySoft.RateLimit　　　　　　　　　　　中文](#fireflysoftratelimit中文)
  - [Introduction](#introduction)
  - [Features](#features)
  - [Projects](#projects)
  - [Usage](#usage)
    - [ASP.NET Core](#aspnet-core)
    - [ASP.NET](#aspnet)
    - [Others](#others)

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

## Projects
| Project                                  | Descriptioin                                           |
| ---------------------------------------- | ------------------------------------------------------ |
| FireflySoft.RateLmit.Core                | algorithm, rules, persistence and other core codes.    |
| FireflySoft.RateLimit.AspNet             | ASP.NET rate-limit middleware based on .NET Framework. |
| FireflySoft.RateLimit.AspNetCore         | ASP.NET Core rate-limit middleware.                    |
| FireflySoft.RateLimit.Core.UnitTest      | Unit test for FireflySoft.RateLimit.Core.              |
| FireflySoft.RateLimit.Core.BenchmarkTest | Benchmark test for FireflySoft.RateLimit.Core.         |
| Samples/Console                          | FireflySoft.RateLmit.Core sample program.              |
| Samples/AspNet                           | FireflySoft.RateLimit.AspNet sample program.          |
| Samples/AspNetCore                       | FireflySoft.RateLimit.AspNetCore sample program.      |
| Samples/RuleAutoUpdate                | A sample that can automatic update rate limiting rules. |
## Usage

### ASP.NET Core

***1、Install Nuget Package***

Package Manager:

```shell
Install-Package FireflySoft.RateLimit.AspNetCore
```

Or .NET CLI:

```shell
dotnet add package FireflySoft.RateLimit.AspNetCore
```

Or Project file：
```xml
<ItemGroup>
<PackageReference Include="FireflySoft.RateLimit.AspNetCore" Version="2.*" />
</ItemGroup>
```

***2、Use Middleware***

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

### ASP.NET

***1、Install Nuget Package:***

Package Manager:

```shell
Install-Package FireflySoft.RateLimit.AspNet
```

***2、Register MessageHandler***

Open Global.asax.cs, the following code adds the rate limit message handle:

```csharp
protected void Application_Start()
{
    ...

    GlobalConfiguration.Configuration.MessageHandlers.Add(
        new RateLimitHandler(
            new Core.InProcessAlgorithm.InProcessFixedWindowAlgorithm(
                new[] {
                    new FixedWindowRule()
                    {
                        ExtractTarget = context =>
                        {
                            return (context as HttpRequestMessage).RequestUri.AbsolutePath;
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
        ));

    ...
}
```

### Others

***1、Install Nuget Package***

Package Manager:

```shell
Install-Package FireflySoft.RateLimit.Core
```

Or .NET CLI:

```shell
dotnet add package FireflySoft.RateLimit.Core
```

***2、Use IAlgorithm***

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