namespace ButlerDroid.Core.Scheduling;

public sealed class OnceSchedule : ISchedule
{
	private readonly DateTimeOffset _at;

	public OnceSchedule(string value)
	{
		var parsed = ScheduleTime.ParseOnce(value);
		var local = DateTime.SpecifyKind(parsed, DateTimeKind.Local);
		_at = new DateTimeOffset(local);
	}

	public DateTimeOffset? NextAfter(DateTimeOffset since)
		=> _at > since ? _at : null;
}
