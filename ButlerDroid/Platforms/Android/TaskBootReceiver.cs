using Android.App;
using Android.Content;

namespace ButlerDroid.Platforms.Android;

[BroadcastReceiver(Enabled = true, Exported = true, DirectBootAware = false)]
[IntentFilter([Intent.ActionBootCompleted, Intent.ActionMyPackageReplaced])]
public sealed class TaskBootReceiver : BroadcastReceiver
{
	public override void OnReceive(Context? context, Intent? intent)
	{
		if (intent?.Action is not (Intent.ActionBootCompleted or Intent.ActionMyPackageReplaced))
			return;

		var pending = GoAsync();
		_ = System.Threading.Tasks.Task.Run(async () =>
		{
			try
			{
				await Services.TaskScheduler.RefreshAllAsync();
			}
			finally
			{
				pending?.Finish();
			}
		});
	}
}
