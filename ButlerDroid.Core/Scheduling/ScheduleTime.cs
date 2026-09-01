using System.Globalization;

namespace ButlerDroid.Core.Scheduling;

internal static class ScheduleTime
{
	public static DateTimeOffset FromLocal(int year, int month, int day, int hour, int minute, int second)
	{
		var value = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Unspecified);
		return new DateTimeOffset(value, TimeZoneInfo.Local.GetUtcOffset(value));
	}

	public static DateTime ParseOnce(string value)
	{
		return DateTime.ParseExact(
			value,
			"yyyy-MM-dd HH:mm:ss",
			CultureInfo.InvariantCulture,
			DateTimeStyles.AllowWhiteSpaces);
	}

	public static DateParts ParseMonthDayTime(string value)
	{
		var parsed = DateTime.ParseExact(
			"2000-" + value,
			"yyyy-MM-dd HH:mm:ss",
			CultureInfo.InvariantCulture,
			DateTimeStyles.AllowWhiteSpaces);

		return new DateParts(parsed.Month, parsed.Day, parsed.Hour, parsed.Minute, parsed.Second);
	}

	public static DateTime CreateLocalDate(int year, int month, int day, int hour, int minute, int second)
	{
		var normalizedDay = Math.Min(day, DateTime.DaysInMonth(year, month));
		return new DateTime(year, month, normalizedDay, hour, minute, second, DateTimeKind.Unspecified);
	}

	internal readonly record struct DateParts(int Month, int Day, int Hour, int Minute, int Second);
}
