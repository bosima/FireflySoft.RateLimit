using FireflySoft.RateLimit.Core;
using FireflySoft.RateLimit.Core.InProcessAlgorithm;
using FireflySoft.RateLimit.Core.Rule;

namespace aspnetcore6.RateLimit;

public class AutoUpdateAlgorithmManager
{
    readonly RateLimitConfiguratioinManager _configurationManager;
    readonly IAlgorithm _algorithm;

    public AutoUpdateAlgorithmManager(RateLimitConfiguratioinManager configurationManager)
    {
        _configurationManager = configurationManager;
        _algorithm = new InProcessTokenBucketAlgorithm(new TokenBucketRule[0],updatable:true);
        _configurationManager.Watch(UpdateAlgorithmRules);
    }

    public IAlgorithm GetAlgorithmInstance()
    {
        return _algorithm;
    }

    private void UpdateAlgorithmRules(IEnumerable<RateLimitConfiguration> configurations)
    {
        var rules = ConvertConfigurationsToRules(configurations);
        _algorithm.UpdateRules(rules);
    }

    private IEnumerable<TokenBucketRule> ConvertConfigurationsToRules(IEnumerable<RateLimitConfiguration> configurations)
    {
        List<TokenBucketRule> ruleList = new List<TokenBucketRule>();
        foreach (var configuration in configurations)
        {
            var tokenRule = new TokenBucketRule(configuration.TokenCapacity, configuration.TokenSpeed, TimeSpan.FromSeconds(1))
            {
                ExtractTarget = context =>
                {
                    var requestSymbol = ExtractRequestSymbol((HttpContext)context);

                    return configuration.PathType == LimitPathType.Single ?
                    requestSymbol.Item1 + "," + requestSymbol.Item2 :
                    requestSymbol.Item1;
                },
                CheckRuleMatching = context =>
                {
                    var requestSymbol = ExtractRequestSymbol((HttpContext)context);

                    return configuration.PathType == LimitPathType.Single ?
                    configuration.Path == requestSymbol.Item2 :
                    !string.IsNullOrWhiteSpace(requestSymbol.Item1);
                },
                Name = $"The Rule for '{configuration.Path}'",
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