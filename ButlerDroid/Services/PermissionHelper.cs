using Android;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Microsoft.Maui.ApplicationModel;

namespace ButlerDroid.Services;

public static class PermissionHelper
{
	private const string StartupPermissionStepKey = "startup_permission_step";
	private static readonly SemaphoreSlim PermissionFlowGate = new(1, 1);

	public static async Task ResumeStartupPermissionFlowAsync()
	{
		var page = Shell.Current?.CurrentPage;
		if (page is null)
			return;

		await EnsureAllStartupPermissionsAsync(page);
	}

	public static async Task EnsureAllStartupPermissionsAsync(Page page)
	{
		await PermissionFlowGate.WaitAsync();
		try
		{
			var step = Preferences.Get(StartupPermissionStepKey, 0);
			if (step >= 4)
				return;

			switch (step)
			{
				case 0:
					await EnsurePostNotificationsAsync(page);
					Preferences.Set(StartupPermissionStepKey, 1);
					break;

				case 1:
					if (!HasExactAlarmPermission())
					{
						await page.DisplayAlertAsync(
							"允许精确闹钟",
							"接下来需要打开系统设置，允许 ButlerDroid 使用精确闹钟，否则定时提醒可能不准。",
							"去设置");
						OpenExactAlarmSettings();
						Preferences.Set(StartupPermissionStepKey, 2);
						return;
					}

					Preferences.Set(StartupPermissionStepKey, 2);
					break;

				case 2:
					if (!IsIgnoringBatteryOptimizations())
					{
						await page.DisplayAlertAsync(
							"允许后台运行",
							"接下来请允许 ButlerDroid 忽略电池优化，以便后台提醒更稳定。",
							"去设置");
						OpenBatteryOptimizationSettings();
						Preferences.Set(StartupPermissionStepKey, 3);
						return;
					}

					Preferences.Set(StartupPermissionStepKey, 3);
					break;

				case 3:
					if (!OperatingSystem.IsAndroidVersionAtLeast(26))
					{
						Preferences.Set(StartupPermissionStepKey, 4);
						break;
					}

					await page.DisplayAlertAsync(
						"开启锁屏和横幅提醒",
						"为了让定时提醒能在锁屏和屏幕顶部弹出，请打开通知设置，并允许“横幅/悬浮通知”和“锁屏通知”。",
						"去设置");
					LocalNotificationService.EnsureChannel();
					OpenTaskNotificationSettings();
					Preferences.Set(StartupPermissionStepKey, 4);
					break;
			}
		}
		finally
		{
			PermissionFlowGate.Release();
		}
	}

	public static void ResetStartupPermissionFlow()
	{
		Preferences.Set(StartupPermissionStepKey, 0);
	}

	public static async Task<bool> EnsurePostNotificationsAsync(Page? page = null)
	{
		if (!OperatingSystem.IsAndroidVersionAtLeast(33))
			return true;

		var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
		if (status == PermissionStatus.Granted)
			return true;

		status = await Permissions.RequestAsync<Permissions.PostNotifications>();
		if (status == PermissionStatus.Granted)
			return true;

		if (page is not null)
			await page.DisplayAlertAsync("通知权限未开启", "本地提醒需要通知权限才能弹出系统通知。", "确定");

		return false;
	}

	public static bool HasExactAlarmPermission()
	{
		if (!OperatingSystem.IsAndroidVersionAtLeast(31))
			return true;

		var manager = (AlarmManager?)Android.App.Application.Context
			.GetSystemService(Context.AlarmService);
		return manager?.CanScheduleExactAlarms() == true;
	}

	public static bool IsIgnoringBatteryOptimizations()
	{
		var context = Android.App.Application.Context;
		var power = (PowerManager?)context.GetSystemService(Context.PowerService);
		return power?.IsIgnoringBatteryOptimizations(context.PackageName!) == true;
	}

	public static void OpenNotificationSettings()
	{
		var context = Android.App.Application.Context;
		var intent = new Intent(Android.Provider.Settings.ActionAppNotificationSettings);
		intent.PutExtra(Android.Provider.Settings.ExtraAppPackage, context.PackageName);
		intent.AddFlags(ActivityFlags.NewTask);
		context.StartActivity(intent);
	}

	public static void OpenTaskNotificationSettings()
	{
		var context = Android.App.Application.Context;
		var intent = new Intent(Android.Provider.Settings.ActionChannelNotificationSettings);
		intent.PutExtra(Android.Provider.Settings.ExtraAppPackage, context.PackageName);
		intent.PutExtra(Android.Provider.Settings.ExtraChannelId, LocalNotificationService.TaskChannelId);
		intent.AddFlags(ActivityFlags.NewTask);
		context.StartActivity(intent);
	}

	public static void OpenExactAlarmSettings()
	{
		if (!OperatingSystem.IsAndroidVersionAtLeast(31))
			return;

		var context = Android.App.Application.Context;
		var intent = new Intent(Android.Provider.Settings.ActionRequestScheduleExactAlarm);
		intent.SetData(Android.Net.Uri.Parse("package:" + context.PackageName));
		intent.AddFlags(ActivityFlags.NewTask);
		context.StartActivity(intent);
	}

	public static void OpenBatteryOptimizationSettings()
	{
		var context = Android.App.Application.Context;
		var intent = new Intent(Android.Provider.Settings.ActionRequestIgnoreBatteryOptimizations);
		intent.SetData(Android.Net.Uri.Parse("package:" + context.PackageName));
		intent.AddFlags(ActivityFlags.NewTask);
		context.StartActivity(intent);
	}
}
