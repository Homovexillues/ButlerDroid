namespace ButlerDroid.Core.Scheduling;

public sealed class IntervalSchedule : ISchedule
{
	private readonly TimeSpan _interval;
	private readonly DateTimeOffset _anchor;

	public IntervalSchedule(TimeSpan interval, DateTimeOffset anchor)
	{
		if (interval <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be positive.");

		_interval = interval;
		_anchor = anchor;
	}

	public DateTimeOffset? NextAfter(DateTimeOffset since)
	{
		if (since < _anchor)
			return _anchor;

		var elapsedTicks = (since - _anchor).Ticks;
		var periods = elapsedTicks / _interval.Ticks + 1;
		return _anchor.AddTicks(periods * _interval.Ticks);
	}
}
