using Cronos;

namespace ButlerDroid.Core.Scheduling;

public sealed class CronSchedule : ISchedule
{
	private static readonly string[] WeekdayNames = ["周日", "周一", "周二", "周三", "周四", "周五", "周六"];

	private readonly CronExpression _expression;

	public CronSchedule(string value)
	{
		_expression = CronExpression.Parse(value, CronFormat.Standard);
	}

	public DateTimeOffset? NextAfter(DateTimeOffset since)
		=> _expression.GetNextOccurrence(since, TimeZoneInfo.Local, inclusive: false);

	public static string Describe(string value)
	{
		var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (parts.Length != 5)
			return "循环任务";

		var minute = parts[0];
		var hour = parts[1];
		var dayOfMonth = parts[2];
		var month = parts[3];
		var dayOfWeek = parts[4];

		if (minute == "*" && hour == "*" && dayOfMonth == "*" && month == "*" && dayOfWeek == "*")
			return "每分钟";

		if (minute == "0" && hour == "*" && dayOfMonth == "*" && month == "*" && dayOfWeek == "*")
			return "每小时";

		if (int.TryParse(minute, out var everyDayMinute)
			&& int.TryParse(hour, out var everyDayHour)
			&& dayOfMonth == "*"
			&& month == "*"
			&& dayOfWeek == "*")
		{
			return $"每天 {everyDayHour:00}:{everyDayMinute:00}";
		}

		if (int.TryParse(minute, out var weeklyMinute)
			&& int.TryParse(hour, out var weeklyHour)
			&& dayOfMonth == "*"
			&& month == "*"
			&& int.TryParse(dayOfWeek, out var weeklyDay)
			&& weeklyDay is >= 0 and <= 6)
		{
			return $"每周{WeekdayNames[weeklyDay]} {weeklyHour:00}:{weeklyMinute:00}";
		}

		if (int.TryParse(minute, out var monthlyMinute)
			&& int.TryParse(hour, out var monthlyHour)
			&& int.TryParse(dayOfMonth, out var monthlyDay)
			&& month == "*"
			&& dayOfWeek == "*")
		{
			return $"每月 {monthlyDay} 日 {monthlyHour:00}:{monthlyMinute:00}";
		}

		if (TryParseInterval(minute, out var minuteInterval)
			&& hour == "*"
			&& dayOfMonth == "*"
			&& month == "*"
			&& dayOfWeek == "*")
		{
			return $"每 {minuteInterval} 分钟";
		}

		if (minute == "0"
			&& TryParseInterval(hour, out var hourInterval)
			&& dayOfMonth == "*"
			&& month == "*"
			&& dayOfWeek == "*")
		{
			return $"每 {hourInterval} 小时";
		}

		if (int.TryParse(minute, out var dailyIntervalMinute)
			&& int.TryParse(hour, out var dailyIntervalHour)
			&& TryParseInterval(dayOfMonth, out var dayInterval)
			&& month == "*"
			&& dayOfWeek == "*")
		{
			return $"每 {dayInterval} 天 {dailyIntervalHour:00}:{dailyIntervalMinute:00}";
		}

		return "循环任务";
	}

	private static bool TryParseInterval(string value, out int interval)
	{
		interval = 0;
		if (!value.StartsWith("*/", StringComparison.Ordinal))
			return false;

		return int.TryParse(value.AsSpan(2), out interval) && interval > 0;
	}
}
