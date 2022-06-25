using RuleAutoUpdate.RateLimit;
using FireflySoft.RateLimit.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add firefly soft rate limit service
builder.Services.AddAutoUpdateRateLimitAlgorithm();
builder.Services.AddRateLimit(serviceProvider =>
{
    var algorithmManager = serviceProvider.GetService<AutoUpdateAlgorithmManager>();
    if (algorithmManager != null)
    {
        return algorithmManager.GetAlgorithmInstance();
    }

    return null;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

// Use firefly soft rate limit middleware
app.UseRateLimit();

app.MapControllers();

app.Run();
