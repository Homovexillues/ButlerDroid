using System.Collections.ObjectModel;
using ButlerDroid.Core.Models;
using ButlerDroid.Services;
using ButlerScheduler = ButlerDroid.Services.TaskScheduler;

namespace ButlerDroid.Pages;

public partial class TaskListPage : ContentPage
{
	private readonly ObservableCollection<ScheduledTask> _tasks = [];

	public TaskListPage()
	{
		InitializeComponent();
		TasksView.ItemsSource = _tasks;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await PermissionHelper.EnsureAllStartupPermissionsAsync(this);
		await SyncTasksAsync();
	}

	private async Task SyncTasksAsync()
	{
		var incoming = await TaskStore.GetAllAsync();
		var incomingIds = incoming.Select(task => task.Id).ToHashSet();

		for (var i = _tasks.Count - 1; i >= 0; i--)
		{
			if (!incomingIds.Contains(_tasks[i].Id))
				_tasks.RemoveAt(i);
		}

		var existingIndexes = _tasks
			.Select((task, index) => (task.Id, Index: index))
			.ToDictionary(item => item.Id, item => item.Index);

		foreach (var task in incoming)
		{
			if (existingIndexes.TryGetValue(task.Id, out var index))
			{
				_tasks[index] = task;
			}
			else
			{
				_tasks.Add(task);
			}
		}
	}

	private async void OnNewTask(object sender, EventArgs e)
		=> await Shell.Current.GoToAsync(nameof(TaskEditPage));

	private async void OnExportTasks(object sender, EventArgs e)
	{
		try
		{
			await TaskTransferService.ExportAsync(_tasks);
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("导出失败", ex.Message, "确定");
		}
	}

	private async void OnImportTasks(object sender, EventArgs e)
	{
		try
		{
			var result = await TaskTransferService.ImportAsync();
			await SyncTasksAsync();
			await DisplayAlertAsync(
				"导入完成",
				$"新增 {result.Created} 个，更新 {result.Updated} 个。",
				"确定");
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("导入失败", ex.Message, "确定");
		}
	}

	private async void OnEditTask(object sender, EventArgs e)
	{
		if ((sender as Button)?.CommandParameter is ScheduledTask task)
			await Shell.Current.GoToAsync($"{nameof(TaskEditPage)}?taskId={task.Id}");
	}

	private async void OnTestTask(object sender, EventArgs e)
	{
		if ((sender as Button)?.CommandParameter is not ScheduledTask task)
			return;

		await ButlerScheduler.TriggerAsync(task.Id, markFired: false);
	}

	private async void OnTaskEnabledToggled(object sender, ToggledEventArgs e)
	{
		if ((sender as Switch)?.BindingContext is not ScheduledTask task)
			return;

		task.IsEnabled = e.Value;
		await TaskStore.SaveAsync(task);
		await ButlerScheduler.ScheduleAsync(task);
		await RefreshSingleTaskAsync(task.Id);
		await ButlerScheduler.RefreshAllAsync();
	}

	private async void OnDeleteTask(object sender, EventArgs e)
	{
		if ((sender as Button)?.CommandParameter is not ScheduledTask task)
			return;

		var confirmed = await DisplayAlertAsync("删除提醒", $"确定删除「{task.Title}」吗？", "删除", "取消");
		if (!confirmed)
			return;

		ButlerScheduler.CancelAlarm(task.Id);
		SpeechService.DeleteTaskAudio(task.Id);
		await TaskStore.DeleteAsync(task.Id);
		var index = IndexOfTask(task.Id);
		if (index >= 0)
			_tasks.RemoveAt(index);

		await ButlerScheduler.RefreshAllAsync();
	}

	private async Task RefreshSingleTaskAsync(int taskId)
	{
		var task = await TaskStore.GetAsync(taskId);
		if (task is null)
			return;

		var index = IndexOfTask(taskId);
		if (index >= 0)
		{
			_tasks[index] = task;
		}
		else
		{
			_tasks.Add(task);
		}
	}

	private int IndexOfTask(int taskId)
	{
		for (var i = 0; i < _tasks.Count; i++)
		{
			if (_tasks[i].Id == taskId)
				return i;
		}

		return -1;
	}
}
