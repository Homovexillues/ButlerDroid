using Android.App;
using Android.Content;
using ButlerDroid.Core.Models;

namespace ButlerDroid.Services;

public static class TaskScheduler
{
	public const string AlarmAction = "com.companyname.butlerdroid.TASK_ALARM";
	public const string ServiceStartAction = "com.companyname.butlerdroid.SCHEDULER_START";
	public const string ServiceStopAction = "com.companyname.butlerdroid.SCHEDULER_STOP";

	public static async Task RefreshAllAsync(bool startServiceIfNeeded = true)
	{
		await Database.InitializeAsync();
		var tasks = await TaskStore.GetAllAsync();
		var anyEnabled = false;

		foreach (var task in tasks)
		{
			if (!task.IsEnabled)
			{
				CancelAlarm(task.Id);
				continue;
			}

			var scheduled = await ScheduleAsync(task, save: false);
			anyEnabled |= scheduled;
		}

		if (anyEnabled && startServiceIfNeeded)
			StartForegroundService();
		else if (!anyEnabled)
			StopForegroundService();
	}

	public static async Task<bool> ScheduleAsync(ScheduledTask task, bool save = false)
	{
		if (!task.IsEnabled)
		{
			CancelAlarm(task.Id);
			return false;
		}

		DateTimeOffset? next;
		try
		{
			next = ButlerDroid.Core.Scheduling.ScheduleFactory.NextAfter(task, DateTimeOffset.Now);
		}
		catch
		{
			CancelAlarm(task.Id);
			return false;
		}

		if (next is null)
		{
			CancelAlarm(task.Id);
			return false;
		}

		SetAlarm(task.Id, next.Value);
		if (save)
			await TaskStore.SaveAsync(task);
		return true;
	}

	public static async Task TriggerAsync(int taskId, bool markFired = true)
	{
		await Database.InitializeAsync();
		var task = await TaskStore.GetAsync(taskId);
		if (task is null || !task.IsEnabled)
			return;

		LocalNotificationService.Show(task.Title, task.Body);
		await SpeechService.PlayPreparedAsync(task.Id, task.Title, task.Body);

		if (markFired)
		{
			task.LastFiredAtUnixMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();
			await TaskStore.SaveAsync(task);
		}

		await ScheduleAsync(task);
		StartForegroundService();
	}

	public static void CancelAlarm(int taskId)
	{
		var manager = (AlarmManager?)Android.App.Application.Context
			.GetSystemService(Context.AlarmService);
		manager?.Cancel(BuildAlarmIntent(taskId));
	}

	private static void SetAlarm(int taskId, DateTimeOffset triggerAt)
	{
		var context = Android.App.Application.Context;
		var manager = (AlarmManager?)context.GetSystemService(Context.AlarmService);
		if (manager is null)
			return;

		var triggerAtMs = triggerAt.ToUnixTimeMilliseconds();
		var pendingIntent = BuildAlarmIntent(taskId);

		try
		{
			if (OperatingSystem.IsAndroidVersionAtLeast(23))
				manager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, triggerAtMs, pendingIntent);
			else if (OperatingSystem.IsAndroidVersionAtLeast(19))
				manager.SetExact(AlarmType.RtcWakeup, triggerAtMs, pendingIntent);
			else
				manager.Set(AlarmType.RtcWakeup, triggerAtMs, pendingIntent);
		}
		catch (Java.Lang.SecurityException)
		{
			manager.Set(AlarmType.RtcWakeup, triggerAtMs, pendingIntent);
		}
	}

	private static PendingIntent BuildAlarmIntent(int taskId)
	{
		var context = Android.App.Application.Context;
		var intent = new Intent(context, typeof(Platforms.Android.TaskAlarmReceiver));
		intent.SetAction(AlarmAction);
		intent.PutExtra("taskId", taskId);
		return PendingIntent.GetBroadcast(
			context,
			taskId,
			intent,
			PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
	}

	public static void StartForegroundService()
	{
		var context = Android.App.Application.Context;
		var intent = new Intent(context, typeof(Platforms.Android.SchedulerForegroundService));
		intent.SetAction(ServiceStartAction);

		if (OperatingSystem.IsAndroidVersionAtLeast(26))
			context.StartForegroundService(intent);
		else
			context.StartService(intent);
	}

	public static void StopForegroundService()
	{
		var context = Android.App.Application.Context;
		var intent = new Intent(context, typeof(Platforms.Android.SchedulerForegroundService));
		intent.SetAction(ServiceStopAction);
		context.StartService(intent);
	}
}
