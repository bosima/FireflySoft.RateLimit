using System.Collections.ObjectModel;

namespace aspnetcore6.RateLimit;

public class RateLimitConfiguration
{
    public string? Path { get; set; }
    public LimitPathType PathType { get; set; }
    public int TokenCapacity { get; set; }
    public int TokenSpeed { get; set; }
}

public enum LimitPathType
{
    Single = 1,
    All = 2
}

public class RateLimitConfiguratioinManager
{
    readonly RateLimitRuleDAO _dao;

    public RateLimitConfiguratioinManager(RateLimitRuleDAO dao)
    {
        _dao = dao;
    }

    public async Task<IEnumerable<RateLimitConfiguration>> LoadAsync()
    {
        var rules = await _dao.GetAllRulesAsync();

        return rules.Select(d =>
        {
            return new RateLimitConfiguration()
            {
                Path = d.Path,
                PathType = d.PathType,
                TokenCapacity = d.TokenCapacity,
                TokenSpeed = d.TokenSpeed
            };
        });
    }
}