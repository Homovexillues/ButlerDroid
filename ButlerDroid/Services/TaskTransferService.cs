using System.Text.Json;
using System.Text.Json.Serialization;
using ButlerDroid.Core.Models;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Storage;

namespace ButlerDroid.Services;

public static class TaskTransferService
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true,
		WriteIndented = true,
	};

	public static async Task ExportAsync(IEnumerable<ScheduledTask> tasks)
	{
		var document = new TaskExportDocument
		{
			Version = 2,
			Tasks = tasks.Select(ToDto).ToList(),
		};

		var json = JsonSerializer.Serialize(document, JsonOptions);
		var path = Path.Combine(FileSystem.CacheDirectory, "butlerdroid-tasks.json");
		await File.WriteAllTextAsync(path, json);

		await Share.Default.RequestAsync(new ShareFileRequest
		{
			Title = "ButlerDroid 任务",
			File = new ShareFile(path),
		});
	}

	public static async Task<TaskImportResult> ImportAsync()
	{
		var result = await FilePicker.Default.PickAsync(new PickOptions
		{
			PickerTitle = "选择 ButlerDroid 任务 JSON",
		});

		if (result is null)
			return new TaskImportResult(0, 0);

		var json = await File.ReadAllTextAsync(result.FullPath);
		var document = JsonSerializer.Deserialize<TaskExportDocument>(json, JsonOptions)
			?? throw new JsonException("无法解析任务 JSON。");

		if (document.Version is < 1 or > 2)
			throw new NotSupportedException($"不支持的任务文件版本：{document.Version}。");

		var created = 0;
		var updated = 0;
		foreach (var dto in document.Tasks)
		{
			var task = ToTask(dto);
			var existing = await TaskStore.GetByKeyAsync(task.TaskKey);
			if (existing is null)
			{
				await TaskStore.SaveAsync(task);
				created++;
			}
			else
			{
				task.Id = existing.Id;
				task.CreatedAtUnixMs = existing.CreatedAtUnixMs;
				task.LastFiredAtUnixMs = existing.LastFiredAtUnixMs;
				await TaskStore.SaveAsync(task);
				updated++;
			}

			try
			{
				await SpeechService.PrepareTaskAudioAsync(task.Id, task.Title, task.Body);
			}
			catch
			{
				// 音频准备失败不阻止导入。
			}
		}

		await TaskScheduler.RefreshAllAsync();
		return new TaskImportResult(created, updated);
	}

	private static TaskExportDto ToDto(ScheduledTask task)
	{
		return new TaskExportDto
		{
			TaskKey = task.TaskKey,
			Title = task.Title,
			Body = task.Body,
			Kind = task.Kind.ToString(),
			ScheduleValue = task.ScheduleValue,
			Offsets = task.Offsets.ToList(),
			IsEnabled = task.IsEnabled,
			IntervalSeconds = task.IntervalSeconds,
			AnchorAtUnixMs = task.AnchorAtUnixMs,
		};
	}

	private static ScheduledTask ToTask(TaskExportDto dto)
	{
		if (!Enum.TryParse<ScheduleKind>(dto.Kind, ignoreCase: true, out var kind))
			throw new JsonException($"未知调度类型：{dto.Kind}");

		return new ScheduledTask
		{
			TaskKey = dto.TaskKey ?? "",
			Title = dto.Title ?? "",
			Body = dto.Body ?? "",
			Kind = kind,
			ScheduleValue = dto.ScheduleValue ?? "",
			Offsets = dto.Offsets ?? [],
			IsEnabled = dto.IsEnabled,
			IntervalSeconds = dto.IntervalSeconds,
			AnchorAtUnixMs = dto.AnchorAtUnixMs,
		};
	}

	private sealed class TaskExportDocument
	{
		public int Version { get; set; }
		public List<TaskExportDto> Tasks { get; set; } = [];
	}

	private sealed class TaskExportDto
	{
		public string? TaskKey { get; set; }
		public string? Title { get; set; }
		public string? Body { get; set; }
		public string? Kind { get; set; }
		public string? ScheduleValue { get; set; }
		public List<string>? Offsets { get; set; }
		public bool IsEnabled { get; set; } = true;
		public long IntervalSeconds { get; set; }
		public long AnchorAtUnixMs { get; set; }
	}

	public sealed record TaskImportResult(int Created, int Updated);
}
