namespace ButlerDroid;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(Pages.TaskEditPage), typeof(Pages.TaskEditPage));
	}
}
