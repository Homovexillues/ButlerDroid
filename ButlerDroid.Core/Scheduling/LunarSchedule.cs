using ChineseLunar = Lunar.Lunar;

namespace ButlerDroid.Core.Scheduling;

public sealed class LunarSchedule : ISchedule
{
	private readonly int _month;
	private readonly int _day;
	private readonly int _hour;
	private readonly int _minute;
	private readonly int _second;

	public LunarSchedule(string value)
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

	private DateTimeOffset MakeDate(int lunarYear)
	{
		var lunar = new ChineseLunar(lunarYear, _month, _day, _hour, _minute, _second);
		var solar = lunar.Solar;
		var value = ScheduleTime.CreateLocalDate(
			solar.Year,
			solar.Month,
			solar.Day,
			_hour,
			_minute,
			_second);

		return new DateTimeOffset(value, TimeZoneInfo.Local.GetUtcOffset(value));
	}
}
