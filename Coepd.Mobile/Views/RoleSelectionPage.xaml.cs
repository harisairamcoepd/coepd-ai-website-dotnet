using Coepd.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Coepd.Mobile.Views;

public partial class RoleSelectionPage : ContentPage
{
    private readonly IServiceProvider _serviceProvider;

    public RoleSelectionPage(RoleSelectionViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _serviceProvider = serviceProvider;
    }

    private async Task OpenLoginAsync(string role)
    {
        var page = _serviceProvider.GetRequiredService<LoginPage>();
        page.ConfigureRole(role);
        await Navigation.PushAsync(page);
    }

    private async void OnAdminClicked(object sender, EventArgs e)
    {
        await OpenLoginAsync("admin");
    }

    private async void OnStaffClicked(object sender, EventArgs e)
    {
        await OpenLoginAsync("staff");
    }
}
