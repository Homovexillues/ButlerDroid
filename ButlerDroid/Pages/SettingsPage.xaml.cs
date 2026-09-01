using ButlerDroid.Services;
using ButlerScheduler = ButlerDroid.Services.TaskScheduler;

namespace ButlerDroid.Pages;

public partial class SettingsPage : ContentPage
{
	public SettingsPage()
	{
		InitializeComponent();
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		SpeechSwitch.IsToggled = SpeechService.IsEnabled;
		RefreshStatus();
	}

	private void OnSpeechToggled(object sender, ToggledEventArgs e)
	{
		SpeechService.IsEnabled = e.Value;
		StatusLabel.Text = e.Value ? "语音播报已开启。" : "语音播报已关闭。";
	}

	private async void OnRunPermissionFlow(object sender, EventArgs e)
	{
		PermissionHelper.ResetStartupPermissionFlow();
		await PermissionHelper.EnsureAllStartupPermissionsAsync(this);
		RefreshStatus();
	}

	private void OnStartService(object sender, EventArgs e)
	{
		ButlerScheduler.StartForegroundService();
		StatusLabel.Text = "调度服务启动请求已发送。";
	}

	private void OnStopService(object sender, EventArgs e)
	{
		ButlerScheduler.StopForegroundService();
		StatusLabel.Text = "调度服务停止请求已发送。";
	}

	private void RefreshStatus()
	{
		var exact = PermissionHelper.HasExactAlarmPermission();
		var battery = PermissionHelper.IsIgnoringBatteryOptimizations();
		StatusLabel.Text = $"精确闹钟：{(exact ? "已开启" : "未开启")}，电池优化：{(battery ? "已关闭" : "未关闭")}。";
	}
}
