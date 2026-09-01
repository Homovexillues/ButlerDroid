namespace ButlerDroid.Core.Scheduling;

public sealed class SolarAnnualSchedule : ISchedule
{
	private readonly int _month;
	private readonly int _day;
	private readonly int _hour;
	private readonly int _minute;
	private readonly int _second;

	public SolarAnnualSchedule(string value)
	{
		var parts = ScheduleTime.ParseMonthDayTime(value);
		_month = parts.Month;
		_day = parts.Day;
		_hour = parts.Hour;
		_minute = parts.Minute;
		_second = parts.Second;
	}

	public DateTimeOffset? NextAfter(DateTimeOffset since)
	{
		var next = MakeDate(since.Year);
		if (next <= since)
			next = MakeDate(since.Year + 1);
		return next;
	}

	private DateTimeOffset MakeDate(int year)
	{
		var value = ScheduleTime.CreateLocalDate(year, _month, _day, _hour, _minute, _second);
		return new DateTimeOffset(value, TimeZoneInfo.Local.GetUtcOffset(value));
	}
}
