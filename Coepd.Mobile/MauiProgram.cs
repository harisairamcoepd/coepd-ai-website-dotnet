using Coepd.Mobile.Services;
using Coepd.Mobile.ViewModels;
using Coepd.Mobile.Views;
using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Devices;

namespace Coepd.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		if (DeviceInfo.Platform != DevicePlatform.Android || DeviceInfo.Version.Major >= 6)
		{
			builder.Services.AddMauiBlazorWebView();
		}

#if DEBUG
		builder.Logging.AddDebug();
		if (DeviceInfo.Platform != DevicePlatform.Android || DeviceInfo.Version.Major >= 6)
		{
			builder.Services.AddBlazorWebViewDeveloperTools();
		}
#endif

		builder.Services.AddSingleton<ApiSession>();
		builder.Services.AddSingleton<AuthService>();
		builder.Services.AddSingleton<LeadApiService>();
		builder.Services.AddSingleton<NotificationService>();
		builder.Services.AddSingleton<LeadMonitorService>();

		builder.Services.AddTransient<RoleSelectionViewModel>();
		builder.Services.AddTransient<LoginViewModel>();
		builder.Services.AddTransient<DashboardViewModel>();
		builder.Services.AddTransient<LeadsViewModel>();

		builder.Services.AddTransient<SplashPage>();
		builder.Services.AddTransient<RoleSelectionPage>();
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<DashboardPage>();
		builder.Services.AddTransient<LeadsPage>();
		builder.Services.AddTransient<AlertsPage>();
		builder.Services.AddTransient<SettingsPage>();
		builder.Services.AddTransient<RazorCommandCenterPage>();
		builder.Services.AddTransient<AppShell>();

		return builder.Build();
	}
}
