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
		await ReloadAsync();
	}

	private async Task ReloadAsync()
	{
		var tasks = await TaskStore.GetAllAsync();
		_tasks.Clear();
		foreach (var task in tasks)
			_tasks.Add(task);
	}

	private async void OnNewTask(object sender, EventArgs e)
		=> await Shell.Current.GoToAsync(nameof(TaskEditPage));

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
		await ReloadAsync();
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
		await ReloadAsync();
		await ButlerScheduler.RefreshAllAsync();
	}
}
