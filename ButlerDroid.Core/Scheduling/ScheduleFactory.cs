using ButlerDroid.Core.Models;

namespace ButlerDroid.Core.Scheduling;

public static class ScheduleFactory
{
	public static ISchedule Create(ScheduledTask task)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(task.Title);

		if (task.Kind == ScheduleKind.Interval)
		{
			if (task.IntervalSeconds <= 0)
				throw new ArgumentException("Interval seconds must be positive.", nameof(task.IntervalSeconds));

			return new IntervalSchedule(task.Interval, task.AnchorAt);
		}

		ArgumentException.ThrowIfNullOrWhiteSpace(task.ScheduleValue);

		ISchedule baseSchedule = task.Kind switch
		{
			ScheduleKind.Once => new OnceSchedule(task.ScheduleValue),
			ScheduleKind.Solar => new SolarAnnualSchedule(task.ScheduleValue),
			ScheduleKind.Lunar => new LunarSchedule(task.ScheduleValue),
			ScheduleKind.Cron => new CronSchedule(task.ScheduleValue),
			_ => throw new InvalidOperationException($"Unsupported schedule kind: {task.Kind}"),
		};

		var offsets = task.Offsets;
		return offsets.Count == 0
			? baseSchedule
			: new OffsetSchedule(baseSchedule, offsets.Select(ScheduleOffsetParser.Parse));
	}

	public static DateTimeOffset? NextAfter(ScheduledTask task, DateTimeOffset since)
		=> Create(task).NextAfter(since);
}
