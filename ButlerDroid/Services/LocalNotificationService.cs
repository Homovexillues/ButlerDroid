using Android.App;
using Android.Content;
using AndroidX.Core.App;

namespace ButlerDroid.Services;

public static class LocalNotificationService
{
	public const string TaskChannelId = "butler_task_channel_v2";
	private static int _notificationId = 3000;

	public static void Show(string title, string body)
	{
		EnsureChannel();
		var context = Android.App.Application.Context!;
		var pendingIntent = BuildLaunchIntent(context);
		var notification = new NotificationCompat.Builder(context, TaskChannelId)
			.SetContentTitle(string.IsNullOrWhiteSpace(title) ? "ButlerDroid" : title)
			.SetContentText(body)
			.SetStyle(new NotificationCompat.BigTextStyle().BigText(body))
			.SetSmallIcon(global::ButlerDroid.Resource.Drawable.ic_stat_timer)
			.SetLargeIcon(NotificationIconHelper.LoadBitmap(global::ButlerDroid.Resource.Drawable.ic_stat_timer_large))
			.SetAutoCancel(true)
			.SetPriority(NotificationCompat.PriorityMax)
			.SetCategory(NotificationCompat.CategoryAlarm)
			.SetVisibility(NotificationCompat.VisibilityPublic)
			.SetDefaults((int)NotificationDefaults.All)
			.SetContentIntent(pendingIntent)
			.Build();

		NotificationManagerCompat.From(context)
			.Notify(System.Threading.Interlocked.Increment(ref _notificationId), notification);
	}

	public static void EnsureChannel()
	{
		if (!OperatingSystem.IsAndroidVersionAtLeast(26))
			return;

		var manager = (NotificationManager?)Android.App.Application.Context
			.GetSystemService(Context.NotificationService);
		if (manager?.GetNotificationChannel(TaskChannelId) is not null)
			return;

		var channel = new NotificationChannel(TaskChannelId, "定时提醒", NotificationImportance.Max)
		{
			Description = "本地定时任务触发的系统通知",
			LockscreenVisibility = NotificationVisibility.Public,
		};
		channel.EnableVibration(true);
		channel.EnableLights(true);
		channel.SetShowBadge(true);
		manager?.CreateNotificationChannel(channel);
	}

	private static PendingIntent? BuildLaunchIntent(Context context)
	{
		var launch = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName!);
		if (launch is null)
			return null;

		launch.AddFlags(ActivityFlags.SingleTop);
		var launchIntent = launch ?? new Intent(context, typeof(MainActivity));
		return PendingIntent.GetActivity(
			context,
			1,
			launchIntent,
			PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
	}
}
