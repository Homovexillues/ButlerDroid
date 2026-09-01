using Android.Media;
using Android.Speech.Tts;
using AndroidTts = Android.Speech.Tts.TextToSpeech;

namespace ButlerDroid.Services;

public static class SpeechService
{
	private const string AudioDirectoryName = "TaskAudio";
	private static readonly SemaphoreSlim PlaybackGate = new(1, 1);
	private static AndroidTts? _speech;
	private static TaskCompletionSource<bool>? _initCompletion;
	private static bool _initialized;
	public static string? LastPrepareError { get; private set; }

	public static bool IsEnabled
	{
		get => Preferences.Get("speech_enabled", true);
		set => Preferences.Set("speech_enabled", value);
	}

	public static void Prewarm()
	{
		if (!IsEnabled)
			return;

		_ = EnsureInitializedAsync();
	}

	private static string AudioRootDirectory
	{
		get
		{
			var external = Android.App.Application.Context.GetExternalFilesDir(null)?.AbsolutePath;
			return string.IsNullOrWhiteSpace(external) ? FileSystem.AppDataDirectory : external;
		}
	}

	public static string GetTaskAudioPath(int taskId)
		=> Path.Combine(AudioRootDirectory, AudioDirectoryName, $"task-{taskId}.wav");

	public static bool HasPreparedAudio(int taskId)
		=> File.Exists(GetTaskAudioPath(taskId));

	public static async Task PrepareTaskAudioAsync(int taskId, string title, string body)
	{
		if (!IsEnabled)
			return;

		var text = GetSpeechText(title, body);
		if (string.IsNullOrWhiteSpace(text))
		{
			DeleteTaskAudio(taskId);
			return;
		}

		await RunOnMainAsync(() => SynthesizeTaskAudioAsync(taskId, text));
	}

	public static async Task PlayPreparedAsync(int taskId, string title, string body)
	{
		if (!IsEnabled)
			return;

		var path = GetTaskAudioPath(taskId);
		if (File.Exists(path))
		{
			await PlayAudioAsync(path);
			return;
		}

		await SpeakAsync(title, body);
	}

	public static async Task EnsureAllTaskAudioAsync()
	{
		if (!IsEnabled)
			return;

		var tasks = await TaskStore.GetAllAsync();
		foreach (var task in tasks)
		{
			if (!task.IsEnabled || HasPreparedAudio(task.Id))
				continue;

			await PrepareTaskAudioAsync(task.Id, task.Title, task.Body);
		}
	}

	public static void DeleteTaskAudio(int taskId)
	{
		try
		{
			var path = GetTaskAudioPath(taskId);
			if (File.Exists(path))
				File.Delete(path);
		}
		catch
		{
		}
	}

	public static async Task SpeakAsync(string title, string body)
	{
		if (!IsEnabled)
			return;

		var text = GetSpeechText(title, body);
		if (string.IsNullOrWhiteSpace(text))
			return;

		await RunOnMainAsync(() => SpeakOnMainThreadAsync(text));
	}

	public static void Stop()
	{
		_speech?.Stop();
	}

	private static string GetSpeechText(string title, string body)
		=> string.IsNullOrWhiteSpace(body) ? title : body;

	private static Task RunOnMainAsync(Func<Task> action)
	{
		var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(async () =>
		{
			try
			{
				await action();
				completion.TrySetResult(true);
			}
			catch (Exception ex)
			{
				completion.TrySetException(ex);
			}
		});
		return completion.Task;
	}

	private static async Task SynthesizeTaskAudioAsync(int taskId, string text)
	{
		try
		{
			await EnsureInitializedAsync();
			if (_speech is null)
				throw new InvalidOperationException("系统 TTS 初始化失败。");

			var finalPath = GetTaskAudioPath(taskId);
			var tempPath = finalPath + ".tmp";
			Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);
			File.Delete(finalPath);
			File.Delete(tempPath);

			var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			var listener = new SynthesisProgressListener(completion);
			_speech.SetOnUtteranceProgressListener(listener);
			_speech.SetLanguage(Java.Util.Locale.SimplifiedChinese);
			_speech.SetAudioAttributes(CreateSpeechAudioAttributes());

			var result = _speech.SynthesizeToFile(
				text,
				null,
				new Java.IO.File(tempPath),
				$"butler-task-audio-{taskId}");

			if (result != OperationResult.Success)
			{
				File.Delete(tempPath);
				throw new IOException("TTS 文件合成请求失败。");
			}

			await completion.Task;
			_speech.SetOnUtteranceProgressListener(null);

			if (File.Exists(tempPath))
				File.Move(tempPath, finalPath);

			LastPrepareError = null;
		}
		catch (Exception ex)
		{
			LastPrepareError = ex.Message;
			throw;
		}
	}

	private static async Task SpeakOnMainThreadAsync(string text)
	{
		await EnsureInitializedAsync();
		if (_speech is null)
			return;

		_speech.SetLanguage(Java.Util.Locale.SimplifiedChinese);
		_speech.SetAudioAttributes(CreateSpeechAudioAttributes());
		_speech.Speak(text, QueueMode.Flush, null, $"butler-task-{Guid.NewGuid():N}");
	}

	private static async Task PlayAudioAsync(string path)
	{
		await PlaybackGate.WaitAsync();
		try
		{
			var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			var player = new MediaPlayer();
			player.SetAudioAttributes(CreateMediaAudioAttributes());
			player.SetDataSource(path);
			player.Completion += (_, _) => completion.TrySetResult(true);
			player.Error += (_, _) => completion.TrySetException(new IOException("音频播放失败。"));

			try
			{
				await Task.Run(player.Prepare);
				player.Start();
				await completion.Task;
			}
			finally
			{
				player.Completion -= (_, _) => { };
				player.Error -= (_, _) => { };
				player.Release();
				player.Dispose();
			}
		}
		finally
		{
			PlaybackGate.Release();
		}
	}

	private static async Task EnsureInitializedAsync()
	{
		if (_initialized && _speech is not null)
			return;

		if (_initCompletion is not null)
		{
			await _initCompletion.Task;
			return;
		}

		var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		_initCompletion = completion;
		try
		{
			var context = Android.App.Application.Context;
			AndroidTts speech = null!;
			speech = new AndroidTts(context, new InitListener((status) =>
			{
				_initialized = status == OperationResult.Success;
				_speech = _initialized ? speech : null;
				if (_initialized)
				{
					_speech.SetLanguage(Java.Util.Locale.SimplifiedChinese);
					_speech.SetAudioAttributes(CreateSpeechAudioAttributes());
				}
				completion.TrySetResult(_initialized);
			}));
		}
		catch (Exception ex)
		{
			_initCompletion = null;
			_initialized = false;
			_speech = null;
			completion.TrySetException(ex);
		}

		await completion.Task;
	}

	private static AudioAttributes CreateSpeechAudioAttributes()
	{
		return new AudioAttributes.Builder()
			.SetUsage(AudioUsageKind.Media)
			.SetContentType(AudioContentType.Speech)
			.Build()!;
	}

	private static AudioAttributes CreateMediaAudioAttributes()
	{
		return new AudioAttributes.Builder()
			.SetUsage(AudioUsageKind.Media)
			.SetContentType(AudioContentType.Speech)
			.Build()!;
	}

	private sealed class InitListener : Java.Lang.Object, AndroidTts.IOnInitListener
	{
		private readonly Action<OperationResult> _onInit;

		public InitListener(Action<OperationResult> onInit)
		{
			_onInit = onInit;
		}

		public void OnInit(OperationResult status)
			=> _onInit(status);
	}

	private sealed class SynthesisProgressListener : UtteranceProgressListener
	{
		private readonly TaskCompletionSource<bool> _completion;

		public SynthesisProgressListener(TaskCompletionSource<bool> completion)
		{
			_completion = completion;
		}

		public override void OnStart(string? utteranceId)
		{
		}

		public override void OnDone(string? utteranceId)
		{
			_completion.TrySetResult(true);
		}

		[Obsolete]
		public override void OnError(string? utteranceId)
		{
			_completion.TrySetException(new IOException("TTS 文件合成失败。"));
		}
	}
}
