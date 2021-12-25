using System;
using System.Threading.Tasks;
using FireflySoft.RateLimit.Core.Time;

namespace FireflySoft.RateLimit.Core.Test
{
    internal class TestTimeProvider : ITimeProvider
    {
		private DateTimeOffset _startTime;

		private TimeSpan _interval;

		public TestTimeProvider(TimeSpan interval)
		{
			_startTime = DateTimeOffset.Parse("2022-01-01T00:00:00+00:00");
			_interval = interval;
		}

		public long GetCurrentUtcMilliseconds()
		{
			return _startTime.ToUnixTimeMilliseconds();
		}

		public DateTimeOffset GetCurrentUtcTime()
		{
			return _startTime;
		}

		public DateTimeOffset GetCurrentLocalTime()
		{
			return _startTime.ToLocalTime();
		}

		public Task<long> GetCurrentUtcMillisecondsAsync()
		{
			return Task.FromResult<long>(GetCurrentUtcMilliseconds());
		}

		public Task<DateTimeOffset> GetCurrentLocalTimeAsync()
		{
			return Task.FromResult<DateTimeOffset>(GetCurrentLocalTime());
		}

		public Task<DateTimeOffset> GetCurrentUtcTimeAsync()
		{
			return Task.FromResult<DateTimeOffset>(GetCurrentUtcTime());
		}

		public void Increment()
		{
			_startTime = _startTime.Add(_interval);
		}

		public void Increment(TimeSpan interval)
		{
			_startTime = _startTime.Add(interval);
		}

		public void IncrementMilliseconds(int milliseconds)
		{
			Increment(TimeSpan.FromMilliseconds(milliseconds));
		}

		public void IncrementSeconds(int seconds)
		{
			Increment(TimeSpan.FromSeconds(seconds));
		}
	}
}