using Android.Graphics;
using AndroidX.Core.Content;

namespace ButlerDroid.Services;

public static class NotificationIconHelper
{
	public static Bitmap? LoadBitmap(int resourceId)
	{
		var context = Android.App.Application.Context;
		var drawable = ContextCompat.GetDrawable(context, resourceId);
		if (drawable is null)
			return null;

		const int size = 96;
		var bitmap = Bitmap.CreateBitmap(size, size, Bitmap.Config.Argb8888!);
		var canvas = new Canvas(bitmap);
		drawable.SetBounds(0, 0, size, size);
		drawable.Draw(canvas);
		return bitmap;
	}

	public static Bitmap? LoadAppIcon()
	{
		var context = Android.App.Application.Context;
		return BitmapFactory.DecodeResource(context.Resources, global::ButlerDroid.Resource.Mipmap.appicon);
	}
}
