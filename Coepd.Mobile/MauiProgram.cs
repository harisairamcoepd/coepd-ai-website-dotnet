using Coepd.Mobile.Services;
using Coepd.Mobile.ViewModels;
using Coepd.Mobile.Views;
using Microsoft.Extensions.Logging;

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

#if DEBUG
		builder.Logging.AddDebug();
#endif

		builder.Services.AddSingleton<ApiSession>();
		builder.Services.AddSingleton<AuthService>();
		builder.Services.AddSingleton<LeadApiService>();

		builder.Services.AddTransient<RoleSelectionViewModel>();
		builder.Services.AddTransient<LoginViewModel>();
		builder.Services.AddTransient<DashboardViewModel>();
		builder.Services.AddTransient<LeadsViewModel>();

		builder.Services.AddTransient<SplashPage>();
		builder.Services.AddTransient<RoleSelectionPage>();
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<DashboardPage>();
		builder.Services.AddTransient<LeadsPage>();

		return builder.Build();
	}
}
