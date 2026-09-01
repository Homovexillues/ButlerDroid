using Android.App;
using Android.Content;
using Microsoft.Maui.ApplicationModel;

namespace ButlerDroid.Services;

public static class ForegroundAlertService
{
	private static readonly object SyncRoot = new();
	private static Task _lastAlert = Task.CompletedTask;

	public static void Show(string title, string body)
	{
		MainThread.BeginInvokeOnMainThread(async () =>
		{
			if (!MainActivity.IsResumed)
				return;

			Task previous;
			lock (SyncRoot)
			{
				previous = _lastAlert;
			}

			var completion = new TaskCompletionSource<bool>(
				TaskCreationOptions.RunContinuationsAsynchronously);

			lock (SyncRoot)
			{
				_lastAlert = completion.Task;
			}

			try
			{
				await previous;
			}
			catch
			{
				// A previous alert may have failed because the activity disappeared.
			}

			var activity = Platform.CurrentActivity;
			if (activity is null || activity.IsFinishing)
			{
				completion.TrySetResult(true);
				return;
			}

			try
			{
				var builder = new AlertDialog.Builder(activity)
					.SetTitle(string.IsNullOrWhiteSpace(title) ? "ButlerDroid" : title)
					.SetMessage(string.IsNullOrWhiteSpace(body) ? title : body)
					.SetCancelable(true)
					.SetPositiveButton("知道了", (_, _) => { });

				builder.SetOnDismissListener(new DismissListener(completion));
				builder.Show();
				await Task.WhenAny(completion.Task, Task.Delay(TimeSpan.FromSeconds(20)));
				completion.TrySetResult(true);
			}
			catch
			{
				completion.TrySetResult(true);
			}
		});
	}

	private sealed class DismissListener : Java.Lang.Object, IDialogInterfaceOnDismissListener
	{
		private readonly TaskCompletionSource<bool> _completion;

		public DismissListener(TaskCompletionSource<bool> completion)
		{
			_completion = completion;
		}

		public void OnDismiss(IDialogInterface? dialog)
			=> _completion.TrySetResult(true);
	}
}
