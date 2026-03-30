using Coepd.Mobile.Views;
using Coepd.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Coepd.Mobile;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;
    private readonly LeadMonitorService _leadMonitorService;

    public App(IServiceProvider serviceProvider, LeadMonitorService leadMonitorService)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        _leadMonitorService = leadMonitorService;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var splashPage = _serviceProvider.GetRequiredService<SplashPage>();
        _ = InitializeNotificationsAsync();
        return new Window(new NavigationPage(splashPage));
    }

    private async Task InitializeNotificationsAsync()
    {
        await _leadMonitorService.StartAsync();
    }
}
