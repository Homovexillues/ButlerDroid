using Android.Content;

namespace ButlerDroid.Platforms.Android;

[BroadcastReceiver(Enabled = true, Exported = false)]
public sealed class TaskAlarmReceiver : BroadcastReceiver
{
	public override void OnReceive(Context? context, Intent? intent)
	{
		if (intent?.Action != Services.TaskScheduler.AlarmAction)
			return;

		var taskId = intent.GetIntExtra("taskId", 0);
		if (taskId <= 0)
			return;

		var pending = GoAsync();
		_ = System.Threading.Tasks.Task.Run(async () =>
		{
			try
			{
				await Services.TaskScheduler.TriggerAsync(taskId);
			}
			finally
			{
				pending?.Finish();
			}
		});
	}
}
