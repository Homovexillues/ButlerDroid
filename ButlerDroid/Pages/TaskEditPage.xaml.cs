using ButlerDroid.Core.Models;
using ButlerDroid.Services;
using ButlerScheduler = ButlerDroid.Services.TaskScheduler;

namespace ButlerDroid.Pages;

[QueryProperty(nameof(TaskId), "taskId")]
public partial class TaskEditPage : ContentPage
{
	private static readonly ScheduleTypeOption[] ScheduleTypes =
	[
		new("一次通知", ScheduleKind.Once),
		new("公历定时", ScheduleKind.Solar),
		new("农历定时", ScheduleKind.Lunar),
		new("循环任务", ScheduleKind.Cron),
	];

	private static readonly string[] LoopModes =
	[
		"每天",
		"每周",
		"每月",
		"固定间隔",
	];

	private static readonly string[] IntervalUnits =
	[
		"分钟",
		"小时",
	];

	private static readonly WeekdayOption[] Weekdays =
	[
		new("周日", 0),
		new("周一", 1),
		new("周二", 2),
		new("周三", 3),
		new("周四", 4),
		new("周五", 5),
		new("周六", 6),
	];

	private int _taskId;
	private readonly System.Collections.ObjectModel.ObservableCollection<OffsetEditorItem> _offsets = [];

	public string TaskId
	{
		set => _taskId = int.TryParse(value, out var id) ? id : 0;
	}

	public TaskEditPage()
	{
		InitializeComponent();

		KindPicker.ItemsSource = ScheduleTypes;
		KindPicker.SelectedIndex = 0;

		CronModePicker.ItemsSource = LoopModes;
		CronModePicker.SelectedIndex = 0;
		CronWeekdayPicker.ItemsSource = Weekdays;
		CronWeekdayPicker.SelectedIndex = 1;
		IntervalUnitPicker.ItemsSource = IntervalUnits;
		IntervalUnitPicker.SelectedIndex = 0;
		Microsoft.Maui.Controls.BindableLayout.SetItemsSource(OffsetsContainer, _offsets);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadAsync();
	}

	private async Task LoadAsync()
	{
		await Database.InitializeAsync();
		var task = _taskId == 0 ? null : await TaskStore.GetAsync(_taskId);
		if (task is null)
		{
			Title = "新建提醒";
			OnceDatePicker.Date = DateTime.Today;
			OnceTimePicker.Time = new TimeSpan(9, 0, 0);
			AnnualDatePicker.Date = new DateTime(DateTime.Today.Year, 1, 1);
			AnnualTimePicker.Time = new TimeSpan(9, 0, 0);
			CronTimePicker.Time = new TimeSpan(9, 0, 0);
			IntervalValueEntry.Text = "10";
			EnabledSwitch.IsToggled = true;
			SetOffsets([]);
			UpdateVisibility();
			return;
		}

		Title = "编辑提醒";
		TitleEntry.Text = task.Title;
		BodyEditor.Text = task.Body;
		KindPicker.SelectedItem = task.Kind switch
		{
			ScheduleKind.Once => ScheduleTypes[0],
			ScheduleKind.Solar => ScheduleTypes[1],
			ScheduleKind.Lunar => ScheduleTypes[2],
			ScheduleKind.Cron or ScheduleKind.Interval => ScheduleTypes[3],
			_ => ScheduleTypes[0],
		};
		PopulateScheduleFields(task);
		SetOffsets(task.Offsets);
		EnabledSwitch.IsToggled = task.IsEnabled;
		UpdateVisibility();
	}

	private void OnKindChanged(object? sender, EventArgs e)
		=> UpdateVisibility();

	private void OnCronModeChanged(object? sender, EventArgs e)
		=> UpdateCronVisibility();

	private void UpdateVisibility()
	{
		var kind = SelectedKind;
		OnceFields.IsVisible = kind == ScheduleKind.Once;
		AnnualFields.IsVisible = kind is ScheduleKind.Solar or ScheduleKind.Lunar;
		CronFields.IsVisible = kind == ScheduleKind.Cron;

		if (kind == ScheduleKind.Cron)
			UpdateCronVisibility();
		else
			UpdatePreview();
	}

	private void UpdateCronVisibility()
	{
		var mode = CronModePicker.SelectedIndex;
		var intervalMode = mode == 3;

		IntervalFields.IsVisible = intervalMode;
		CronTimePicker.IsVisible = !intervalMode;
		CronWeekFields.IsVisible = mode == 1;
		CronMonthFields.IsVisible = mode == 2;
		CronModeHint.Text = mode switch
		{
			0 => "在每天指定时间触发一次。",
			1 => "在每周指定星期和时间触发一次。",
			2 => "在每月指定日期和时间触发一次。",
			3 => "从保存时间开始，按固定分钟或小时间隔触发。",
			_ => "",
		};

		UpdatePreview();
	}

	private void PopulateScheduleFields(ScheduledTask task)
	{
		switch (task.Kind)
		{
			case ScheduleKind.Once:
				if (DateTime.TryParseExact(
					task.ScheduleValue,
					"yyyy-MM-dd HH:mm:ss",
					System.Globalization.CultureInfo.InvariantCulture,
					System.Globalization.DateTimeStyles.AllowWhiteSpaces,
					out var once))
				{
					OnceDatePicker.Date = once.Date;
					OnceTimePicker.Time = once.TimeOfDay;
				}
				break;

			case ScheduleKind.Solar:
			case ScheduleKind.Lunar:
				if (DateTime.TryParseExact(
					"2000-" + task.ScheduleValue,
					"yyyy-MM-dd HH:mm:ss",
					System.Globalization.CultureInfo.InvariantCulture,
					System.Globalization.DateTimeStyles.AllowWhiteSpaces,
					out var annual))
				{
					AnnualDatePicker.Date = annual.Date;
					AnnualTimePicker.Time = annual.TimeOfDay;
				}
				break;

			case ScheduleKind.Cron:
				PopulateCronFields(task.ScheduleValue);
				break;

			case ScheduleKind.Interval:
				PopulateIntervalFields(task);
				break;
		}
	}

	private void PopulateCronFields(string cron)
	{
		var parts = cron.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (parts.Length != 5)
			return;

		var minute = parts[0];
		var hour = parts[1];
		var dayOfMonth = parts[2];
		var month = parts[3];
		var dayOfWeek = parts[4];

		if (int.TryParse(minute, out var dailyMinute)
			&& int.TryParse(hour, out var dailyHour)
			&& dayOfMonth == "*"
			&& month == "*"
			&& dayOfWeek == "*")
		{
			CronModePicker.SelectedIndex = 0;
			CronTimePicker.Time = new TimeSpan(dailyHour, dailyMinute, 0);
			return;
		}

		if (int.TryParse(minute, out var weeklyMinute)
			&& int.TryParse(hour, out var weeklyHour)
			&& dayOfMonth == "*"
			&& month == "*"
			&& int.TryParse(dayOfWeek, out var weeklyDay))
		{
			CronModePicker.SelectedIndex = 1;
			CronTimePicker.Time = new TimeSpan(weeklyHour, weeklyMinute, 0);
			CronWeekdayPicker.SelectedItem = Weekdays.FirstOrDefault(item => item.CronDay == weeklyDay)
				?? Weekdays[1];
			return;
		}

		if (int.TryParse(minute, out var monthlyMinute)
			&& int.TryParse(hour, out var monthlyHour)
			&& int.TryParse(dayOfMonth, out var monthlyDay)
			&& month == "*"
			&& dayOfWeek == "*")
		{
			CronModePicker.SelectedIndex = 2;
			CronTimePicker.Time = new TimeSpan(monthlyHour, monthlyMinute, 0);
			CronMonthDayEntry.Text = monthlyDay.ToString();
			return;
		}

		CronModePicker.SelectedIndex = 0;
		CronTimePicker.Time = new TimeSpan(9, 0, 0);
	}

	private void PopulateIntervalFields(ScheduledTask task)
	{
		CronModePicker.SelectedIndex = 3;
		var interval = task.Interval;
		if (interval.TotalHours >= 1 && interval.TotalHours % 1 == 0)
		{
			IntervalUnitPicker.SelectedIndex = 1;
			IntervalValueEntry.Text = ((int)interval.TotalHours).ToString();
		}
		else
		{
			IntervalUnitPicker.SelectedIndex = 0;
			IntervalValueEntry.Text = ((int)interval.TotalMinutes).ToString();
		}
	}

	private ScheduleKind SelectedKind
		=> KindPicker.SelectedItem is ScheduleTypeOption option ? option.Kind : ScheduleKind.Once;

	private bool IsIntervalLoopSelected
		=> SelectedKind == ScheduleKind.Cron && CronModePicker.SelectedIndex == 3;

	private async void OnSave(object sender, EventArgs e)
	{
		var title = TitleEntry.Text?.Trim() ?? "";
		if (string.IsNullOrWhiteSpace(title))
		{
			StatusLabel.Text = "标题不能为空。";
			return;
		}

		var task = _taskId == 0 ? new ScheduledTask() : await TaskStore.GetAsync(_taskId);
		if (task is null)
			task = new ScheduledTask();

		task.Title = title;
		task.Body = BodyEditor.Text?.Trim() ?? "";
		task.IsEnabled = EnabledSwitch.IsToggled;
		task.Offsets = BuildOffsets();

		try
		{
			ApplyScheduleToTask(task);
			_ = ButlerDroid.Core.Scheduling.ScheduleFactory.NextAfter(task, DateTimeOffset.Now);
		}
		catch (Exception ex)
		{
			StatusLabel.Text = $"配置无效：{ex.Message}";
			return;
		}

		await TaskStore.SaveAsync(task);
		try
		{
			await SpeechService.PrepareTaskAudioAsync(task.Id, task.Title, task.Body);
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("音频生成失败", $"任务已保存，但语音文件生成失败：{ex.Message}", "确定");
		}
		await ButlerScheduler.ScheduleAsync(task);
		await ButlerScheduler.RefreshAllAsync();
		await Shell.Current.GoToAsync("..");
	}

	private void ApplyScheduleToTask(ScheduledTask task)
	{
		switch (SelectedKind)
		{
			case ScheduleKind.Once:
				task.Kind = ScheduleKind.Once;
				task.ScheduleValue = BuildOnceValue();
				break;

			case ScheduleKind.Solar:
				task.Kind = ScheduleKind.Solar;
				task.ScheduleValue = BuildAnnualValue();
				break;

			case ScheduleKind.Lunar:
				task.Kind = ScheduleKind.Lunar;
				task.ScheduleValue = BuildAnnualValue();
				break;

			case ScheduleKind.Cron:
				if (IsIntervalLoopSelected)
				{
					task.Kind = ScheduleKind.Interval;
					task.IntervalSeconds = ParseIntervalSeconds();
					task.AnchorAtUnixMs = task.AnchorAtUnixMs == 0
						? DateTimeOffset.Now.ToUnixTimeMilliseconds()
						: task.AnchorAtUnixMs;
					task.ScheduleValue = "";
				}
				else
				{
					task.Kind = ScheduleKind.Cron;
					task.ScheduleValue = BuildCronValue();
					task.IntervalSeconds = 0;
					task.AnchorAtUnixMs = 0;
				}
				break;

			default:
				throw new InvalidOperationException("未知调度类型。");
		}
	}

	private string BuildOnceValue()
	{
		var date = (OnceDatePicker.Date ?? DateTime.Today).Date + (OnceTimePicker.Time ?? TimeSpan.Zero);
		return date.ToString("yyyy-MM-dd HH:mm:ss");
	}

	private string BuildAnnualValue()
	{
		var date = AnnualDatePicker.Date ?? DateTime.Today;
		var time = AnnualTimePicker.Time ?? TimeSpan.Zero;
		return $"{date:MM-dd} {FormatTime(time)}";
	}

	private string BuildCronValue()
	{
		var mode = CronModePicker.SelectedIndex;
		var time = CronTimePicker.Time ?? TimeSpan.Zero;
		var minute = time.Minutes.ToString();
		var hour = time.Hours.ToString();

		return mode switch
		{
			0 => $"{minute} {hour} * * *",
			1 => $"{minute} {hour} * * {SelectedWeekday}",
			2 => $"{minute} {hour} {ParseMonthDay()} * *",
			3 => throw new InvalidOperationException("固定间隔不生成 Cron 表达式。"),
			_ => throw new InvalidOperationException("未知循环频率。"),
		};
	}

	private long ParseIntervalSeconds()
	{
		if (!int.TryParse(IntervalValueEntry.Text, out var value) || value <= 0)
			throw new FormatException("间隔数值必须是正整数。");

		return IntervalUnitPicker.SelectedIndex switch
		{
			0 => value * 60L,
			1 => value * 3600L,
			_ => throw new FormatException("间隔单位必须是分钟或小时。"),
		};
	}

	private int SelectedWeekday
		=> CronWeekdayPicker.SelectedItem is WeekdayOption option ? option.CronDay : 1;

	private int ParseMonthDay()
	{
		if (!int.TryParse(CronMonthDayEntry.Text, out var day) || day is < 1 or > 31)
			throw new FormatException("每月日期必须是 1 到 31。");
		return day;
	}

	private void OnAddOffset(object sender, EventArgs e)
		=> _offsets.Add(new OffsetEditorItem());

	private void OnRemoveOffset(object sender, EventArgs e)
	{
		if ((sender as Button)?.CommandParameter is OffsetEditorItem item)
			_offsets.Remove(item);
	}

	private void SetOffsets(IEnumerable<string> offsets)
	{
		_offsets.Clear();
		foreach (var offset in offsets)
		{
			var item = ParseOffsetItem(offset);
			if (item is not null)
				_offsets.Add(item);
		}
	}

	private static OffsetEditorItem? ParseOffsetItem(string offset)
	{
		if (string.IsNullOrWhiteSpace(offset))
			return null;

		var match = System.Text.RegularExpressions.Regex.Match(offset, @"^T-(\d+)([dhm])$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
		if (!match.Success)
			return null;

		return new OffsetEditorItem
		{
			Value = match.Groups[1].Value,
			Unit = match.Groups[2].Value.ToLowerInvariant() switch
			{
				"d" => "天",
				"h" => "小时",
				"m" => "分钟",
				_ => "天",
			},
		};
	}

	private IReadOnlyList<string> BuildOffsets()
	{
		var result = new List<string>();
		foreach (var item in _offsets)
		{
			if (!int.TryParse(item.Value, out var value) || value < 0)
				throw new FormatException("提前提醒的数值必须是非负整数。");

			var suffix = item.Unit switch
			{
				"小时" => "h",
				"分钟" => "m",
				_ => "d",
			};
			result.Add($"T-{value}{suffix}");
		}
		return result;
	}

	private void UpdatePreview()
	{
		try
		{
			var text = SelectedKind switch
			{
				ScheduleKind.Once => BuildOncePreview(),
				ScheduleKind.Solar => BuildAnnualPreview("公历定时"),
				ScheduleKind.Lunar => BuildAnnualPreview("农历定时"),
				ScheduleKind.Cron when IsIntervalLoopSelected => BuildIntervalPreview(),
				ScheduleKind.Cron => $"循环任务 · {ButlerDroid.Core.Scheduling.CronSchedule.Describe(BuildCronValue())}",
				_ => "定时",
			};
			SchedulePreview.Text = $"预览：{text}";
		}
		catch
		{
			SchedulePreview.Text = "配置尚未完整。";
		}
	}

	private string BuildOncePreview()
	{
		var date = (OnceDatePicker.Date ?? DateTime.Today).Date + (OnceTimePicker.Time ?? TimeSpan.Zero);
		return $"一次通知 · {date:yyyy年M月d日 HH:mm}";
	}

	private string BuildAnnualPreview(string kindName)
	{
		var date = AnnualDatePicker.Date ?? DateTime.Today;
		var time = AnnualTimePicker.Time ?? TimeSpan.Zero;
		return $"{kindName} · {date:M月d日} {FormatClock(time)}";
	}

	private string BuildIntervalPreview()
	{
		var seconds = ParseIntervalSeconds();
		var interval = TimeSpan.FromSeconds(seconds);
		var text = interval.TotalHours >= 1 && interval.TotalHours % 1 == 0
			? $"每 {(int)interval.TotalHours} 小时"
			: $"每 {(int)interval.TotalMinutes} 分钟";
		return $"固定间隔 · {text}";
	}

	private static string FormatTime(TimeSpan time)
		=> $"{time.Hours:00}:{time.Minutes:00}:{time.Seconds:00}";

	private static string FormatClock(TimeSpan time)
		=> time.ToString(@"hh\:mm");

	private async void OnCancel(object sender, EventArgs e)
		=> await Shell.Current.GoToAsync("..");

	private sealed record ScheduleTypeOption(string Label, ScheduleKind Kind)
	{
		public override string ToString() => Label;
	}

	private sealed record WeekdayOption(string Label, int CronDay)
	{
		public override string ToString() => Label;
	}
}
