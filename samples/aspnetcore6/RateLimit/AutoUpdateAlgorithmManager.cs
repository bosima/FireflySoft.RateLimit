using FireflySoft.RateLimit.Core;
using FireflySoft.RateLimit.Core.InProcessAlgorithm;
using FireflySoft.RateLimit.Core.Rule;

namespace aspnetcore6.RateLimit;

public class AutoUpdateAlgorithmManager
{
    readonly RateLimitConfiguratioinManager _configurationManager;
    readonly Timer _updateRuleTimer;
    readonly IAlgorithm _algorithm;

    public AutoUpdateAlgorithmManager(RateLimitConfiguratioinManager configurationManager)
    {
        _configurationManager = configurationManager;

        var rules = GetRulesAsync().Result;
        _algorithm = new InProcessTokenBucketAlgorithm(rules);
        _updateRuleTimer = NonCapturingTimer.Create(new TimerCallback(UpdateRuleTimerCallbackAsync), this, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
    }

    public IAlgorithm GetAlgorithmInstance()
    {
        return _algorithm;
    }

    private async void UpdateRuleTimerCallbackAsync(object? state)
    {
        var rules = await GetRulesAsync();
        await _algorithm.UpdateRulesAsync(rules);
    }

    private async Task<IEnumerable<TokenBucketRule>> GetRulesAsync()
    {
        var configurations = await _configurationManager.LoadAsync();

        List<TokenBucketRule> ruleList = new List<TokenBucketRule>();
        foreach (var configure in configurations)
        {
            var tokenRule = new TokenBucketRule(configure.TokenCapacity, configure.TokenSpeed, TimeSpan.FromSeconds(1))
            {
                ExtractTarget = context =>
                {
                    var requestSymbol = ExtractRequestSymbol((HttpContext)context);

                    return configure.PathType == LimitPathType.Single ?
                    requestSymbol.Item1 + "," + requestSymbol.Item2 :
                    requestSymbol.Item1;
                },
                CheckRuleMatching = context =>
                {
                    var requestSymbol = ExtractRequestSymbol((HttpContext)context);

                    return configure.PathType == LimitPathType.Single ?
                    configure.Path == requestSymbol.Item2 :
                    !string.IsNullOrWhiteSpace(requestSymbol.Item1);
                },
                Name = $"The Rule for '{configure.Path}'",
            };
            ruleList.Add(tokenRule);
        }

        return ruleList;
    }

    public Tuple<string?, string?> ExtractRequestSymbol(HttpContext httpContext)
    {
        var requestUserId = httpContext.Request.Query["UserId"].FirstOrDefault();
        var requestPath = httpContext.Request.Path.Value;
        return new Tuple<string?, string?>(requestUserId, requestPath);
    }
}