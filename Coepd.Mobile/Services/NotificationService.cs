using Microsoft.Maui.ApplicationModel;

namespace Coepd.Mobile.Services;

public class NotificationService
{
    const string ChannelId = "lead_alerts";
    const string ChannelName = "Lead Alerts";

    public async Task EnsurePermissionAsync()
    {
#if ANDROID
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
            if (status != PermissionStatus.Granted)
            {
                await Permissions.RequestAsync<Permissions.PostNotifications>();
            }
        }

        CreateAndroidChannel();
#endif
    }

    public Task ShowNewLeadNotificationAsync(string title, string message)
    {
#if ANDROID
        if (Android.App.Application.Context is not Android.Content.Context appContext ||
            string.IsNullOrWhiteSpace(appContext.PackageName))
        {
            return Task.CompletedTask;
        }

        var manager = appContext.GetSystemService(Android.Content.Context.NotificationService) as Android.App.NotificationManager;
        if (manager == null)
        {
            return Task.CompletedTask;
        }

        CreateAndroidChannel();

        var soundUri = GetNotificationSoundUri();
        var launchIntent = appContext.PackageManager?.GetLaunchIntentForPackage(appContext.PackageName);
        if (launchIntent == null)
        {
            return Task.CompletedTask;
        }

        launchIntent.SetFlags(Android.Content.ActivityFlags.SingleTop | Android.Content.ActivityFlags.ClearTop);

        var pendingFlags = Android.App.PendingIntentFlags.UpdateCurrent;
        if (OperatingSystem.IsAndroidVersionAtLeast(23))
        {
            pendingFlags |= Android.App.PendingIntentFlags.Immutable;
        }

        var pendingIntent = Android.App.PendingIntent.GetActivity(appContext, 1101, launchIntent, pendingFlags);
        if (pendingIntent == null)
        {
            return Task.CompletedTask;
        }

        var builder = new AndroidX.Core.App.NotificationCompat.Builder(appContext, ChannelId)
            .SetContentTitle(title)
            .SetContentText(message)
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetAutoCancel(true)
            .SetPriority(AndroidX.Core.App.NotificationCompat.PriorityHigh)
            .SetDefaults((int)Android.App.NotificationDefaults.All)
            .SetCategory(AndroidX.Core.App.NotificationCompat.CategoryMessage)
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

        if (Android.App.Application.Context is not Android.Content.Context appContext ||
            string.IsNullOrWhiteSpace(appContext.PackageName))
        {
            return;
        }

        var manager = appContext.GetSystemService(Android.Content.Context.NotificationService) as Android.App.NotificationManager;
        if (manager == null)
        {
            return;
        }

        if (manager.GetNotificationChannel(ChannelId) != null)
        {
            return;
        }

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
        channel.SetSound(GetNotificationSoundUri(), audioAttributes);
        manager.CreateNotificationChannel(channel);
    }

    static Android.Net.Uri GetNotificationSoundUri()
    {
        return Android.Media.RingtoneManager.GetDefaultUri(Android.Media.RingtoneType.Notification)
               ?? Android.Provider.Settings.System.DefaultNotificationUri!;
    }
#endif
}
