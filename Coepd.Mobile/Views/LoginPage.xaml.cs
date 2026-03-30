using System.ComponentModel;
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
        _viewModel.ConfigureDefaultsForRole(role);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override void OnDisappearing()
    {
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        base.OnDisappearing();
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        var result = await _viewModel.LoginAsync();
        if (!result.Success)
        {
            return;
        }

        Application.Current!.Windows[0].Page = _serviceProvider.GetRequiredService<AppShell>();
        await Task.CompletedTask;
    }

    private async void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LoginViewModel.ErrorMessage) &&
            !string.IsNullOrWhiteSpace(_viewModel.ErrorMessage))
        {
            await ShakeCardAsync();
        }
    }

    private async Task ShakeCardAsync()
    {
        const uint speed = 50;
        await LoginCard.TranslateTo(-12, 0, speed);
        await LoginCard.TranslateTo(12, 0, speed);
        await LoginCard.TranslateTo(-8, 0, speed);
        await LoginCard.TranslateTo(8, 0, speed);
        await LoginCard.TranslateTo(-4, 0, speed);
        await LoginCard.TranslateTo(0, 0, speed);
    }
}
