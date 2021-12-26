# FireflySoft.RateLimit　　　　　　　　　　　[中文](https://github.com/bosima/FireflySoft.RateLimit/blob/master/README.zh-CN.md)

- [Introduction](#introduction)
- [Features](#features)
- [Projects](#projects)
- [Usage](#usage)
  - [ASP.NET Core](#aspnet-core)
  - [ASP.NET](#aspnet)
  - [Others](#others)
  - [Todo List](#todo-list)

## Introduction
Fireflysoft.RateLimit is a rate limiting library based on .Net standard. Its core is simple and lightweight, and can flexibly meet the rate limiting needs of many scenarios.

## Features
* Multiple rate limiting algorithms: built-in fixed window, sliding window, leaky bucket, token bucket, and can be extended.
* Multiple counting storage: memory and Redis.
* Distributed friendly: Supports unified counting of distributed programs with Redis storage.
* Flexible rate limiting targets: Each data can be extracted from the request to set rate limiting targets.
* Support current limit penalty: the client can be locked for a period of time after the rate limit is triggered.
* Dynamically change the rules: support the dynamic change of the rate limiting rules when the program is running.
* Custom error: You can customize the error code and error message after the current limit is triggered.
* Universality: In principle, it can meet any scenario that requires rate limiting.

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
* Use the same current time for different rules in the same request, including kv expiration processing in redis algorithm.
* Add more Unit tests for the rule change processing logic of various algorithms in the process.
* Add more processing logic for attribute changes of rules for redis algorithms.
* Let ASP.Net Core rate limiting middleware returns the current count information.
