using Android.App;
using Android.Content.PM;
using Android.OS;

namespace ButlerDroid;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
	public static bool IsResumed { get; private set; }

	protected override void OnResume()
	{
		base.OnResume();
		IsResumed = true;
		_ = Services.PermissionHelper.ResumeStartupPermissionFlowAsync();
	}

	protected override void OnPause()
	{
		IsResumed = false;
		base.OnPause();
	}
}
