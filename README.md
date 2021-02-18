* [Introduction](#introduction)
* [Features](#features)
* [Unit Test](#unit-test)
* [Projects](#projects)
* [Usage](#usage)
    * [ASP.NET Core](#aspnet-core)
    * [ASP.NET](#aspnet)
    * [Others](#others)
* [Todo List](#todo-list)

## Introduction
A rate limit library, support .NET Framework and .NET core.

## Features
* Four algorithms: fixed window, sliding window, leaky bucket, token bucket.
* Two counting storages: memory and redis.
* Consistent rate-limit for distributed services.
* Locking for a period of time after triggering the limit.
* Flexible rate-limit target setting and rule matching.
* Custom error number and message.
* Custom rate-limit algorithms and persistence methods.
* Supports updating rules at runtime.
* Any scenario that requires rate-limit.

## Unit Test
Project: FireflySoft.RateLimit.Core.UnitTest

Code coverage:

| Module                     | Line   | Branch | Method |
|---|---|---|---|
| FireflySoft.RateLimit.Core | 90.53% | 80.23% | 95.78% |

## Projects
|Project|Descriptioin|
|---|---
|FireflySoft.RateLmit.Core|algorithm, rules, persistence and other core codes.
|FireflySoft.RateLmit.Core.Sample|FireflySoft.RateLmit.Core sample program.
|FireflySoft.RateLimit.AspNet|ASP.NET rate-limit middleware based on .NET Framework.
|FireflySoft.RateLimit.AspNet.Sample|FireflySoft.RateLimit.AspNet smample program.
|FireflySoft.RateLimit.AspNetCore|ASP.NET Core rate-limit middleware.
|FireflySoft.RateLimit.AspNetCore.Sample|FireflySoft.RateLimit.AspNetCore smample program.

## Usage

### ASP.NET Core

**1、Install Nuget Package**

Package Manager:

```shell
Install-Package FireflySoft.RateLimit.AspNetCore -Version 1.2.0
```

Or .NET CLI:

```shell
dotnet add package FireflySoft.RateLimit.AspNetCore --version 1.2.0
```

Or Project file：
```xml
<ItemGroup>
<PackageReference Include="FireflySoft.RateLimit.AspNetCore" Version="1.2.0" />
</ItemGroup>
```

**2、Use Middleware**

The following code calls the rate-limit [middleware](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-3.1) from Startup.Configure:

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    ...

    app.UseRateLimit(new Core.RateLimitProcessor<HttpContext>.Builder()
        // .WithError(new Core.RateLimitError()
        // {
        //     Code=429,
        //     Message = "The system is busy, please try again later"
        // })
        // .WithStorage(new RedisStorage(StackExchange.Redis.ConnectionMultiplexer.Connect("localhost")))
        .WithAlgorithm(new FixedWindowAlgorithm<HttpContext>( new[] {
            new FixedWindowRateLimitRule<HttpContext>()
            {
                Id = "1",
                ExtractTarget = context =>
                {
                    // for all path, you can customize it
                    return context.Request.Path.Value;
                },
                CheckRuleMatching = context =>
                {
                    // limit every request, you can customize it
                    return true;
                },
                Name="general limit rule",
                LimitNumber=30,
                StatWindow=TimeSpan.FromSeconds(1)
            }
        }))
        .Build());

    ...
}
```

### ASP.NET

***1、Install Nuget Package:***

Package Manager:

```shell
Install-Package FireflySoft.RateLimit.AspNet -Version 1.2.0
```

***2、Register MessageHandler***

Open Global.asax.cs, the following code adds the rate limit message handle:

```csharp
protected void Application_Start()
{
    ...

    GlobalConfiguration.Configuration.MessageHandlers.Add(new RateLimitHandler(
        new Core.RateLimitProcessor<HttpRequestMessage>.Builder()
        // .WithError(new Core.RateLimitError()
        // {
        //     Code=429,
        //     Message = "The system is busy, please try again later"
        // })
        // .WithStorage(new RedisStorage(StackExchange.Redis.ConnectionMultiplexer.Connect("localhost")))
        .WithAlgorithm(new FixedWindowAlgorithm<HttpRequestMessage>( new[] {
            new FixedWindowRateLimitRule<HttpRequestMessage>()
            {
                Id = "2",
                ExtractTarget = context =>
                {
                    // for all path, you can customize it
                    return context.RequestUri.AbsolutePath;
                },
                CheckRuleMatching = context =>
                {
                    // limit every request, you can customize it
                    return true;
                },
                Name="general limit rule",
                LimitNumber=30,
                StatWindow=TimeSpan.FromSeconds(1)
            }
        }))
        .Build()
        ));

    ...
}
```

### Others

**1、Install Nuget Package**

Package Manager:

```shell
Install-Package FireflySoft.RateLimit.Core -Version 1.2.0
```

Or .NET CLI:

```shell
dotnet add package FireflySoft.RateLimit.Core --version 1.2.0
```

**2、Use RateLimitProcessor**

Use *RateLimitProcessor* to filter every request, process the return value of *Check* method.

```csharp
// Rule
var fixedWindowRules = new FixedWindowRateLimitRule<SimulationRequest>[]
    {
        new FixedWindowRateLimitRule<SimulationRequest>()
        {
            Id = "3",
            StatWindow=TimeSpan.FromSeconds(1),
            LimitNumber=30,
            ExtractTarget = (request) =>
            {
                return request.RequestResource;
            },
            CheckRuleMatching = (request) =>
            {
                return true;
            },
        }
    };

// Processor
var processor = new RateLimitProcessor<SimulationRequest>.Builder()
    // .WithError(new RateLimitError(){
    //         Code = 429,
    //         Message = "The system is busy, please try again later"
    // })
    // .WithStorage(new RedisStorage(StackExchange.Redis.ConnectionMultiplexer.Connect("localhost")))
    .WithAlgorithm(new FixedWindowAlgorithm<SimulationRequest>(fixedWindowRules))
    .Build();

/// Check
var result = processor.Check(new SimulationRequest()
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

* Add the current statistical value to the return value of Check method of RateLimitProcessor.
* Add more unit tests.
* Remove IRatelimitStorage and transfer related methods to corresponding algorithm classes.
