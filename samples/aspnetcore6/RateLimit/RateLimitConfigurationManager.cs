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
    readonly Timer _checkConfigChangedTimer;
    DateTime _lastConfigChangedTime;

    Action<IEnumerable<RateLimitConfiguration>>? _action;

    public RateLimitConfiguratioinManager(RateLimitRuleDAO dao)
    {
        _dao = dao;
        _lastConfigChangedTime = DateTime.MinValue;
        _checkConfigChangedTimer = NonCapturingTimer.Create(new TimerCallback(CheckConfigChangedTimerCallbackAsync), this, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(10));
    }

    public void Watch(Action<IEnumerable<RateLimitConfiguration>> action)
    {
        _action = action;
    }

    private async void CheckConfigChangedTimerCallbackAsync(object? state)
    {
        var rules = await _dao.GetAllRulesAsync();
        if (rules.Any())
        {
            var latestChangedTime = rules.OrderByDescending(d => d.UpdateTime).Select(d => d.UpdateTime).First();
            if (latestChangedTime > _lastConfigChangedTime)
            {

                var configs = rules.Select(d =>
                        {
                            return new RateLimitConfiguration()
                            {
                                Path = d.Path,
                                PathType = d.PathType,
                                TokenCapacity = d.TokenCapacity,
                                TokenSpeed = d.TokenSpeed
                            };
                        });

                _action?.Invoke(configs);

                _lastConfigChangedTime = latestChangedTime;
            }
        }
    }
}