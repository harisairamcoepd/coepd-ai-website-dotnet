using Coepd.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Coepd.Mobile.Views;

public partial class LoginPage : ContentPage
{
    private readonly LoginViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;

    public LoginPage(LoginViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _serviceProvider = serviceProvider;
        BindingContext = _viewModel;
    }

    public void ConfigureRole(string role)
    {
        _viewModel.Role = role;
        Title = role.Equals("admin", StringComparison.OrdinalIgnoreCase) ? "Admin Login" : "Staff Login";
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        var result = await _viewModel.LoginAsync();
        if (!result.Success) return;

        var page = _serviceProvider.GetRequiredService<DashboardPage>();
        await Navigation.PushAsync(page);
    }
}
