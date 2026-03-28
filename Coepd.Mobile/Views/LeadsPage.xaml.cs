using Coepd.Mobile.Models;
using Coepd.Mobile.ViewModels;

namespace Coepd.Mobile.Views;

public partial class LeadsPage : ContentPage
{
    private readonly LeadsViewModel _viewModel;
    private bool _loaded;

    public LeadsPage(LeadsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded) return;
        _loaded = true;
        await _viewModel.LoadAsync();
    }

    private async void OnSearchClicked(object sender, EventArgs e)
    {
        await _viewModel.LoadAsync();
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        _viewModel.IsRefreshing = true;
        await _viewModel.LoadAsync();
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not Lead lead) return;
        var confirmed = await DisplayAlert("Delete Lead", $"Delete {lead.Name}?", "Delete", "Cancel");
        if (!confirmed) return;

        try
        {
            await _viewModel.DeleteLeadAsync(lead);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Delete Failed", ex.Message, "Close");
        }
    }
}
