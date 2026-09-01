using ButlerDroid.Core.Models;
using ButlerDroid.Core.Scheduling;

namespace ButlerDroid.Core.Tests;

public sealed class SchedulingTests
{
	private static DateTimeOffset At(int year, int month, int day, int hour = 0, int minute = 0, int second = 0)
	{
		var value = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Local);
		return new DateTimeOffset(value);
	}

	[Fact]
	public void Once_ReturnsFutureDateOnlyOnce()
	{
		var now = At(2026, 6, 17, 12);
		var schedule = new OnceSchedule("2026-06-18 09:00:00");

		var next = schedule.NextAfter(now);

		Assert.Equal(At(2026, 6, 18, 9), next);
		Assert.Null(schedule.NextAfter(next!.Value));
	}

	[Fact]
	public void Once_StrictlyAfterSince()
	{
		var now = At(2026, 6, 17, 9);
		var schedule = new OnceSchedule("2026-06-17 09:00:00");

		Assert.Null(schedule.NextAfter(now));
	}

	[Fact]
	public void SolarAnnual_RollsToNextYear()
	{
		var now = At(2026, 6, 17);
		var schedule = new SolarAnnualSchedule("03-05 09:00:00");

		Assert.Equal(At(2027, 3, 5, 9), schedule.NextAfter(now));
	}

	[Fact]
	public void SolarAnnual_Feb29_FallsBackToFeb28InCommonYear()
	{
		var now = At(2026, 6, 17);
		var schedule = new SolarAnnualSchedule("02-29 09:00:00");

		Assert.Equal(At(2027, 2, 28, 9), schedule.NextAfter(now));
	}

	[Fact]
	public void Cron_ReturnsNextOccurrence()
	{
		var now = At(2026, 6, 17, 12);
		var schedule = new CronSchedule("0 9 * * *");

		Assert.Equal(At(2026, 6, 18, 9), schedule.NextAfter(now));
	}

	[Fact]
	public void OffsetSchedule_ReturnsEarliestReminder()
	{
		var baseSchedule = new SolarAnnualSchedule("03-05 09:00:00");
		var schedule = new OffsetSchedule(
			baseSchedule,
			[ScheduleOffsetParser.Parse("T-3d"), ScheduleOffsetParser.Parse("T-0d")]);

		Assert.Equal(At(2027, 3, 2, 9), schedule.NextAfter(At(2027, 1, 1)));
	}

	[Fact]
	public void OffsetSchedule_DoesNotReturnPastOffsetWhenEarlierOffsetAlreadyPassed()
	{
		var baseSchedule = new SolarAnnualSchedule("03-05 09:00:00");
		var schedule = new OffsetSchedule(
			baseSchedule,
			[ScheduleOffsetParser.Parse("T-3d")]);

		Assert.Equal(At(2028, 3, 2, 9), schedule.NextAfter(At(2027, 3, 4, 9)));
	}

	[Fact]
	public void LunarSchedule_AlwaysAdvances()
	{
		var now = At(2026, 6, 17, 12);
		var schedule = new LunarSchedule("05-08 09:00:00");

		var first = schedule.NextAfter(now);
		var second = schedule.NextAfter(first!.Value);

		Assert.NotNull(first);
		Assert.NotNull(second);
		Assert.True(first.Value > now);
		Assert.True(second.Value > first.Value);
	}

	[Fact]
	public void ScheduleFactory_AppliesOffsets()
	{
		var task = new ScheduledTask
		{
			Title = "生日提醒",
			Kind = ScheduleKind.Solar,
			ScheduleValue = "03-05 09:00:00",
			Offsets = ["T-3d", "T-0d"],
		};

		Assert.Equal(At(2027, 3, 2, 9), ScheduleFactory.NextAfter(task, At(2027, 1, 1)));
	}

	[Fact]
	public void ScheduleFactory_AllowsEmptyScheduleValueForInterval()
	{
		var task = new ScheduledTask
		{
			Title = "固定间隔",
			Kind = ScheduleKind.Interval,
			IntervalSeconds = (long)TimeSpan.FromMinutes(10).TotalSeconds,
			AnchorAtUnixMs = At(2026, 8, 31, 10).ToUnixTimeMilliseconds(),
		};

		Assert.Equal(At(2026, 8, 31, 10, 10), ScheduleFactory.NextAfter(task, At(2026, 8, 31, 10)));
	}

	[Fact]
	public void IntervalSchedule_ReturnsNextSlotAfterSince()
	{
		var anchor = At(2026, 8, 31, 10);
		var schedule = new IntervalSchedule(TimeSpan.FromMinutes(10), anchor);

		Assert.Equal(At(2026, 8, 31, 10, 10), schedule.NextAfter(At(2026, 8, 31, 10, 0)));
		Assert.Equal(At(2026, 8, 31, 10, 20), schedule.NextAfter(At(2026, 8, 31, 10, 10)));
	}

	[Fact]
	public void IntervalSchedule_DoesNotCatchUpMissedSlots()
	{
		var anchor = At(2026, 8, 31, 10);
		var schedule = new IntervalSchedule(TimeSpan.FromMinutes(10), anchor);

		var next = schedule.NextAfter(At(2026, 8, 31, 12, 7));

		Assert.Equal(At(2026, 8, 31, 12, 10), next);
	}

	[Fact]
	public void OffsetParser_RejectsInvalidUnit()
	{
		Assert.Throws<FormatException>(() => ScheduleOffsetParser.Parse("T-3x"));
	}
}
