# FireflySoft.RateLimit　　　　　　　　　　　[English](https://github.com/bosima/FireflySoft.RateLimit)

- [FireflySoft.RateLimit　　　　　　　　　　　English](#fireflysoftratelimitenglish)
  - [介绍](#介绍)
  - [功能](#功能)
  - [项目说明](#项目说明)
  - [使用说明](#使用说明)
    - [ASP.NET Core 应用](#aspnet-core-应用)
    - [ASP.NET 应用](#aspnet-应用)
    - [其它类型应用](#其它类型应用)
    - [待办](#待办)

## 介绍
FireflySoft.RateLimit 是一个基于 .NET Standard 的限流类库，其内核简单轻巧，能够灵活应对各种需求的限流场景。

## 功能
* 多种限流算法：内置固定窗口、滑动窗口、漏桶、令牌桶四种算法，还可自定义扩展。
* 多种计数存储：目前支持内存、Redis（含集群）两种存储方式。
* 分布式友好：通过Redis存储支持分布式程序统一计数。
* 限流目标灵活：可以从请求中提取各种数据用于设置限流目标。
* 支持限流惩罚：可以在客户端触发限流后锁定一段时间不允许其访问。
* 动态更改规则：支持程序运行时动态更改限流规则。
* 自定义错误：可以自定义触发限流后的错误码和错误消息。
* 普适性：原则上可以满足任何需要限流的场景。

## 项目说明
| 项目                                     | 说明                                                   |
| ---------------------------------------- | ------------------------------------------------------ |
| FireflySoft.RateLmit.Core                | 算法、规则等限流核心控制程序。                           |
| FireflySoft.RateLimit.AspNet             | ASP.NET 限流处理器，支持 .NET 4.6.1 及以上版本。         |
| FireflySoft.RateLimit.AspNetCore         | ASP.NET Core 限流中间件，支持 .NET Core 2.0 及后续版本。 |
| FireflySoft.RateLimit.Core.UnitTest      | FireflySoft.RateLimit.Core 的单元测试。                 |
| FireflySoft.RateLimit.Core.BenchmarkTest | FireflySoft.RateLimit.Core 的基准测试。                 |
| samples/console                          | 使用 FireflySoft.RateLmit.Core 的示例程序.              |
| samples/aspnet                           | 使用 FireflySoft.RateLimit.AspNet 的示例程序。          |
| samples/aspnetcore                       | 使用 FireflySoft.RateLimit.AspNetCore 的示例程序。      |
| samples/aspnetcore6                      | 使用 FireflySoft.RateLimit.AspNetCore 的.NET6示例程序。 |

## 使用说明

### ASP.NET Core 应用

***1、安装 Nuget 包***

使用包管理器控制台:

```shell
Install-Package FireflySoft.RateLimit.AspNetCore
```

或者使用 .NET CLI:

```shell
dotnet add package FireflySoft.RateLimit.AspNetCore
```

或者直接添加到项目文件中：
```xml
<ItemGroup>
<PackageReference Include="FireflySoft.RateLimit.AspNetCore" Version="2.*" />
</ItemGroup>
```

***2、使用中间件***

在Startup.cs中注册服务并使用[中间件](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-3.1)：

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
                    // 提取限流目标
                    // 这里是直接从请求中提取Path作为限流目标，还可以多种组合，甚至去远程查询一些数据
                    return (context as HttpContext).Request.Path.Value;
                },
                CheckRuleMatching = context =>
                {
                    // 检查当前请求是否要做限流
                    // 比如有些Url是不做限流的、有些用户是不做限流的
                    return true;
                },
                Name="default limit rule",
                LimitNumber=30, // 限流时间窗口内的最大允许请求数量
                StatWindow=TimeSpan.FromSeconds(1) // 限流计数的时间窗口
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

### ASP.NET 应用

***1、安装 Nuget 包***

使用包管理器控制台：

```shell
Install-Package FireflySoft.RateLimit.AspNet
```

***2、注册消息处理器***

打开 Global.asax.cs，使用下面的代码添加限流处理器：

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

### 其它类型应用

***1、安装 Nuget 包***

使用包管理器控制台：

```shell
Install-Package FireflySoft.RateLimit.Core
```

或者 .NET CLI:

```shell
dotnet add package FireflySoft.RateLimit.Core
```

***2、使用限流算法***

使用 *IAlgorithm* 过滤每个请求, 处理 *Check* 方法的返回值。

```csharp
// 定义限流规则
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

// 使用限流算法
IAlgorithm algorithm = new InProcessFixedWindowAlgorithm(fixedWindowRules);

// 过滤请求
var result = algorithm.Check(new SimulationRequest()
    {
        RequestId = Guid.NewGuid().ToString(),
        RequestResource = "home",
        Parameters = new Dictionary<string, string>() {
                    { "from","sample" },
            }
    });
```

SimulationRequest是一个自定义请求，你可以把它修改为任何适合自己的请求类型。

### 待办
* 同一个请求中的不同限流规则检查使用同一个当前时间。 
* 让ASP.NET Core限流中间件返回当前计数信息。
