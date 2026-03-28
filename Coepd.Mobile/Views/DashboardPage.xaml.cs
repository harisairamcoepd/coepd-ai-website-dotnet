using Coepd.Mobile.Services;
using Coepd.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Coepd.Mobile.Views;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _viewModel;
    private readonly AuthService _authService;
    private readonly IServiceProvider _serviceProvider;
    private bool _loaded;

    public DashboardPage(DashboardViewModel viewModel, AuthService authService, IServiceProvider serviceProvider)
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
        if (_loaded) return;
        _loaded = true;
        await _viewModel.LoadAsync();
    }

    private async void OnOpenLeadsClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(_serviceProvider.GetRequiredService<LeadsPage>());
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await _viewModel.LoadAsync();
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        _authService.Logout();
        await Navigation.PopToRootAsync();
    }
}
