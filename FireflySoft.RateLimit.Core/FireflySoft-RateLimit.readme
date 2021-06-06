Github: https://github.com/bosima/FireflySoft.RateLimit

## Usage

### ASP.NET Core

The following code calls the rate-limit [middleware](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-3.1) from Startup.Configure:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    ...

    app.AddRateLimit(new InProcessFixedWindowAlgorithm(
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