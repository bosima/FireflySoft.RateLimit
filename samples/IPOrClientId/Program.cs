using FireflySoft.RateLimit.AspNetCore;
using FireflySoft.RateLimit.Core.InProcessAlgorithm;
using FireflySoft.RateLimit.Core.Rule;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddRateLimit(new InProcessFixedWindowAlgorithm(
    new[] {
        new FixedWindowRule()
        {
            ExtractTarget = context =>
            {
                var httpContext= context as HttpContext;

                // Through CDN
                var ip = httpContext!.Request.Headers["Cdn-Src-Ip"].FirstOrDefault();
                if (!string.IsNullOrEmpty(ip))
                    return ip;

                // Through SLB
                ip = httpContext!.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (!string.IsNullOrEmpty(ip))
                    return ip;

                ip = httpContext!.Connection.RemoteIpAddress?.ToString();
                return ip??"Anonymous-IP";
            },
            CheckRuleMatching = context =>
            {
                var requestPath = (context as HttpContext)!.Request.Path.Value;
                if (requestPath == "/WeatherForecast/Future")
                {
                    return true;
                }
                return false;
            },
            Name = "ClientIPRule",
            LimitNumber = 3,
            StatWindow = TimeSpan.FromSeconds(1)
        },
        new FixedWindowRule()
        {
            ExtractTarget = context =>
            {
                var httpContext= context as HttpContext;
                var clientID = httpContext!.Request.Headers["X-ClientId"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(clientID))
                {
                    clientID=Guid.NewGuid().ToString();
                    httpContext.Response.Headers.Append("X-ClientId",clientID);
                }

                return clientID??"Anonymous-ClientId";
            },
            CheckRuleMatching = context =>
            {
                var requestPath = (context as HttpContext)!.Request.Path.Value;
                if (requestPath == "/WeatherForecast/Future")
                {
                    return true;
                }
                return false;
            },
            Name = "ClientIdRule",
            LimitNumber = 1,
            StatWindow = TimeSpan.FromSeconds(1)
        }
    })
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseRateLimit();

app.MapControllers();

app.Run();