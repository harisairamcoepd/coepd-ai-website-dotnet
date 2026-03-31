using Coepd.Mobile.Services;
using Coepd.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Coepd.Mobile.Views;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _viewModel;
    private readonly AuthService _authService;
    private readonly IServiceProvider _serviceProvider;
    private bool _isLoggingOut;
    private bool _isPulsing;

    public DashboardPage(DashboardViewModel viewModel, AuthService authService, LeadMonitorService leadMonitorService, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _authService = authService;
        _serviceProvider = serviceProvider;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Start();
        await _viewModel.LoadAsync();
        if (!_isPulsing)
        {
            _isPulsing = true;
            _ = PulseLiveIndicatorAsync();
        }
    }

    protected override void OnDisappearing()
    {
        _viewModel.Stop();
        _isPulsing = false;
        base.OnDisappearing();
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        if (_isLoggingOut)
        {
            return;
        }

        _isLoggingOut = true;
        _viewModel.Stop();
        _authService.Logout();
        Application.Current!.Windows[0].Page = new NavigationPage(_serviceProvider.GetRequiredService<RoleSelectionPage>());
        await Task.CompletedTask;
    }

    private async Task PulseLiveIndicatorAsync()
    {
        while (IsVisible && _isPulsing)
        {
            await LiveDot.FadeTo(0.25, 700, Easing.SinInOut);
            await LiveDot.FadeTo(1, 700, Easing.SinInOut);
        }
    }
}
