using Microsoft.Extensions.DependencyInjection;

namespace ButlerDroid;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		_ = StartBackgroundServicesSafelyAsync();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}

	private static async Task StartBackgroundServicesSafelyAsync()
	{
		try
		{
			await Services.TaskScheduler.RefreshAllAsync();
		}
		catch
		{
			// 启动阶段的调度失败不应让整个应用闪退。
		}
	}
}
