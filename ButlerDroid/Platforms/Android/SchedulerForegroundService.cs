using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using ButlerDroid.Services;

namespace ButlerDroid.Platforms.Android;

[Service(Exported = false, ForegroundServiceType = ForegroundService.TypeSpecialUse | ForegroundService.TypeDataSync)]
public sealed class SchedulerForegroundService : Service
{
	private const string ChannelId = "butler_scheduler_channel_v1";
	private const int NotificationId = 1000;
	private PowerManager.WakeLock? _wakeLock;

	public override IBinder? OnBind(Intent? intent) => null;

	public override void OnCreate()
	{
		base.OnCreate();
		CreateChannel();
	}

	public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
	{
		if (intent?.Action == Services.TaskScheduler.ServiceStopAction)
		{
			StopForeground(StopForegroundFlags.Remove);
			ReleaseWakeLock();
			StopSelf();
			return StartCommandResult.NotSticky;
		}

		StartInForeground("本地定时调度运行中");
		AcquireWakeLock();

		_ = System.Threading.Tasks.Task.Run(async () =>
		{
			try
			{
				await Services.TaskScheduler.RefreshAllAsync(startServiceIfNeeded: false);
			}
			catch
			{
				UpdateForeground("调度刷新失败");
			}
		});

		return StartCommandResult.Sticky;
	}

	public override void OnDestroy()
	{
		ReleaseWakeLock();
		base.OnDestroy();
	}

	private void AcquireWakeLock()
	{
		if (_wakeLock?.IsHeld == true)
			return;

		var power = (PowerManager?)GetSystemService(PowerService);
		_wakeLock = power?.NewWakeLock(WakeLockFlags.Partial, "ButlerDroid::Scheduler");
		if (_wakeLock is not null)
		{
			_wakeLock.SetReferenceCounted(false);
			_wakeLock.Acquire();
		}
	}

	private void ReleaseWakeLock()
	{
		try
		{
			if (_wakeLock?.IsHeld == true)
				_wakeLock.Release();
		}
		catch
		{
		}

		_wakeLock = null;
	}

	private void StartInForeground(string text)
	{
		var notification = BuildForegroundNotification(text);
		if (OperatingSystem.IsAndroidVersionAtLeast(34))
		{
			StartForeground(NotificationId, notification, ForegroundService.TypeSpecialUse);
		}
		else if (OperatingSystem.IsAndroidVersionAtLeast(29))
		{
			StartForeground(NotificationId, notification, ForegroundService.TypeDataSync);
		}
		else
		{
			StartForeground(NotificationId, notification);
		}
	}

	private void UpdateForeground(string text)
	{
		NotificationManagerCompat.From(this)
			.Notify(NotificationId, BuildForegroundNotification(text));
	}

	private Notification BuildForegroundNotification(string text)
	{
		var launch = PackageManager?.GetLaunchIntentForPackage(PackageName!);
		var launchIntent = launch ?? new Intent(this, typeof(MainActivity));
		var pendingIntent = PendingIntent.GetActivity(
			this,
			2,
			launchIntent,
			PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

		return new NotificationCompat.Builder(this, ChannelId)
			.SetContentTitle("ButlerDroid · 定时调度")
			.SetContentText(text)
			.SetSmallIcon(global::ButlerDroid.Resource.Drawable.ic_stat_timer)
			.SetLargeIcon(NotificationIconHelper.LoadBitmap(global::ButlerDroid.Resource.Drawable.ic_stat_timer_large))
			.SetOngoing(true)
			.SetCategory(NotificationCompat.CategoryService)
			.SetPriority(NotificationCompat.PriorityLow)
			.SetContentIntent(pendingIntent)
			.Build();
	}

	private void CreateChannel()
	{
		if (!OperatingSystem.IsAndroidVersionAtLeast(26))
			return;

		var manager = (NotificationManager?)GetSystemService(NotificationService);
		var channel = new NotificationChannel(ChannelId, "定时调度服务", NotificationImportance.Default)
		{
			Description = "保持本地定时任务可靠运行的常驻通知",
		};
		channel.SetSound(null, null);
		channel.EnableVibration(false);
		manager?.CreateNotificationChannel(channel);
	}
}
