# FireflySoft.RateLimit　　　　　　　　　　　　　　[中文](https://github.com/bosima/FireflySoft.RateLimit/blob/master/README.zh-CN.md)

- [Introduction](#introduction)
- [Features](#features)
- [Unit Test](#unit-test)
- [Projects](#projects)
- [Usage](#usage)
  - [ASP.NET Core](#aspnet-core)
  - [ASP.NET](#aspnet)
  - [Others](#others)
  - [Todo List](#todo-list)

## Introduction
A rate limit library, support .NET Framework and .NET core.

## Features
* Four algorithms: fixed window, sliding window, leaky bucket, token bucket.
* Two counting storages: memory and redis.
* Consistent rate-limit for distributed services.
* Locking for a period of time after triggering the limit.
* Flexible rate-limit target setting and rule matching.
* Supports updating rules at runtime.
* Any scenario that requires rate-limit.

## Unit Test
Project: FireflySoft.RateLimit.Core.UnitTest

Code coverage:

| Module                     | Line   | Branch | Method |
| -------------------------- | ------ | ------ | ------ |
| FireflySoft.RateLimit.Core | 95.84% | 85.77% | 99.06% |

## Projects
| Project                                  | Descriptioin                                           |
| ---------------------------------------- | ------------------------------------------------------ |
| FireflySoft.RateLmit.Core                | algorithm, rules, persistence and other core codes.    |
| FireflySoft.RateLimit.AspNet             | ASP.NET rate-limit middleware based on .NET Framework. |
| FireflySoft.RateLimit.AspNetCore         | ASP.NET Core rate-limit middleware.                    |
| FireflySoft.RateLimit.Core.UnitTest      | Unit test for FireflySoft.RateLimit.Core.              |
| FireflySoft.RateLimit.Core.BenchmarkTest | Benchmark test for FireflySoft.RateLimit.Core.         |
| samples/console                          | FireflySoft.RateLmit.Core sample program.              |
| samples/aspnet                           | FireflySoft.RateLimit.AspNet sample program.          |
| samples/aspnetcore                       | FireflySoft.RateLimit.AspNetCore sample program.      |
| samples/aspnetcore6                | FireflySoft.RateLimit.AspNetCore with .NET6 sample program. |
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

### Todo List
* Optimize the time consumption of sliding window algorithm in memory.
* The same current time is used for different rule checking in the same request. 
* Use 'TIME' command in lua script when redis support 'scripts effects replication'.
* Run unit tests in the docker container to avoid installing redis.
