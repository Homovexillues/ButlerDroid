using System.Text.Json;
using SQLite;

namespace ButlerDroid.Core.Models;

public enum ScheduleKind
{
	Once,
	Solar,
	Lunar,
	Cron,
	Interval,
}

public sealed class ScheduledTask
{
	[PrimaryKey, AutoIncrement]
	public int Id { get; set; }

	[Indexed]
	public string TaskKey { get; set; } = "";

	[NotNull]
	public string Title { get; set; } = "";

	public string Body { get; set; } = "";

	public ScheduleKind Kind { get; set; }

	public string ScheduleValue { get; set; } = "";

	public string? OffsetsJson { get; set; }

	public bool IsEnabled { get; set; } = true;

	public long? LastFiredAtUnixMs { get; set; }

	public long CreatedAtUnixMs { get; set; }

	public long UpdatedAtUnixMs { get; set; }

	public long IntervalSeconds { get; set; }

	public long AnchorAtUnixMs { get; set; }

	[Ignore]
	public IReadOnlyList<string> Offsets
	{
		get => string.IsNullOrWhiteSpace(OffsetsJson)
			? []
			: JsonSerializer.Deserialize<List<string>>(OffsetsJson) ?? [];
		set => OffsetsJson = value.Count == 0 ? null : JsonSerializer.Serialize(value);
	}

	[Ignore]
	public DateTimeOffset? LastFiredAt => LastFiredAtUnixMs is long value
		? DateTimeOffset.FromUnixTimeMilliseconds(value).ToLocalTime()
		: null;

	[Ignore]
	public TimeSpan Interval => TimeSpan.FromSeconds(IntervalSeconds);

	[Ignore]
	public DateTimeOffset AnchorAt => AnchorAtUnixMs == 0
		? DateTimeOffset.Now
		: DateTimeOffset.FromUnixTimeMilliseconds(AnchorAtUnixMs).ToLocalTime();

	[Ignore]
	public string ScheduleSummary => Kind switch
	{
		ScheduleKind.Once => $"一次通知 · {ScheduleValue}",
		ScheduleKind.Solar => $"公历定时 · {ScheduleValue}",
		ScheduleKind.Lunar => $"农历定时 · {ScheduleValue}",
		ScheduleKind.Cron => $"循环任务 · {Scheduling.CronSchedule.Describe(ScheduleValue)}",
		ScheduleKind.Interval => $"固定间隔 · {FormatInterval(Interval)}",
		_ => ScheduleValue,
	};

	[Ignore]
	public string? NextFireText
	{
		get
		{
			if (!IsEnabled)
				return "已停用";

			try
			{
				var next = Scheduling.ScheduleFactory.NextAfter(this, DateTimeOffset.Now);
				return next?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "无下次触发";
			}
			catch
			{
				return "调度表达式无效";
			}
		}
	}

	private static string FormatInterval(TimeSpan interval)
	{
		if (interval.TotalHours >= 1 && interval.TotalHours % 1 == 0)
			return $"每 {(int)interval.TotalHours} 小时";
		if (interval.TotalMinutes >= 1)
			return $"每 {(int)interval.TotalMinutes} 分钟";
		return $"每 {interval.TotalSeconds} 秒";
	}
}
