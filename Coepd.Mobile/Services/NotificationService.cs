using Microsoft.Maui.ApplicationModel;

namespace Coepd.Mobile.Services;

public class NotificationService
{
    const string ChannelId = "lead_alerts";
    const string ChannelName = "Lead Alerts";

    public async Task EnsurePermissionAsync()
    {
#if ANDROID
        var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
        if (status != PermissionStatus.Granted)
        {
            await Permissions.RequestAsync<Permissions.PostNotifications>();
        }

        CreateAndroidChannel();
#endif
    }

    public Task ShowNewLeadNotificationAsync(string title, string message)
    {
#if ANDROID
        var context = Android.App.Application.Context;
        var manager = (Android.App.NotificationManager?)context.GetSystemService(Android.Content.Context.NotificationService);
        if (manager == null)
        {
            return Task.CompletedTask;
        }

        CreateAndroidChannel();

        var soundUri = GetNotificationSoundUri(context);

        var launchIntent = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName!);
        launchIntent?.SetFlags(Android.Content.ActivityFlags.SingleTop | Android.Content.ActivityFlags.ClearTop);

        var pendingIntent = Android.App.PendingIntent.GetActivity(
            context,
            1101,
            launchIntent,
            Android.App.PendingIntentFlags.UpdateCurrent | Android.App.PendingIntentFlags.Immutable);

        var builder = new AndroidX.Core.App.NotificationCompat.Builder(context, ChannelId)
            .SetContentTitle(title)
            .SetContentText(message)
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetAutoCancel(true)
            .SetPriority(AndroidX.Core.App.NotificationCompat.PriorityHigh)
            .SetDefaults((int)Android.App.NotificationDefaults.All)
            .SetCategory(AndroidX.Core.App.NotificationCompat.CategoryAlarm)
            .SetSound(soundUri)
            .SetVibrate(new long[] { 0, 180, 120, 180 })
            .SetContentIntent(pendingIntent);

        manager.Notify(1101, builder.Build());
#endif
        return Task.CompletedTask;
    }

#if ANDROID
    static void CreateAndroidChannel()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            return;
        }

        var context = Android.App.Application.Context;
        var manager = (Android.App.NotificationManager?)context.GetSystemService(Android.Content.Context.NotificationService);
        if (manager == null)
        {
            return;
        }

        if (manager.GetNotificationChannel(ChannelId) != null)
        {
            manager.DeleteNotificationChannel(ChannelId);
        }

        var soundUri = GetNotificationSoundUri(context);

        var audioAttributes = new Android.Media.AudioAttributes.Builder()
            .SetUsage(Android.Media.AudioUsageKind.Notification)
            .SetContentType(Android.Media.AudioContentType.Sonification)
            .Build();

        var channel = new Android.App.NotificationChannel(
            ChannelId,
            ChannelName,
            Android.App.NotificationImportance.High)
        {
            Description = "Notifications for newly generated leads."
        };

        channel.EnableLights(true);
        channel.EnableVibration(true);
        channel.SetVibrationPattern(new long[] { 0, 180, 120, 180 });
        channel.SetSound(soundUri, audioAttributes);
        manager.CreateNotificationChannel(channel);
    }

    static Android.Net.Uri GetNotificationSoundUri(Android.Content.Context context)
    {
        var customUri = Android.Net.Uri.Parse(
            $"{Android.Content.ContentResolver.SchemeAndroidResource}://{context.PackageName}/raw/lead_alert");

        return customUri
            ?? Android.Media.RingtoneManager.GetDefaultUri(Android.Media.RingtoneType.Notification)
            ?? Android.Provider.Settings.System.DefaultNotificationUri;
    }
#endif
}
