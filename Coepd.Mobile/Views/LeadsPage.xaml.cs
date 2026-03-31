using Coepd.Mobile.Services;
using Coepd.Mobile.ViewModels;

namespace Coepd.Mobile.Views;

public partial class LeadsPage : ContentPage
{
    private readonly LeadsViewModel _viewModel;
    private readonly LeadMonitorService _leadMonitorService;
    private bool _subscribed;

    public LeadsPage(LeadsViewModel viewModel, LeadMonitorService leadMonitorService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _leadMonitorService = leadMonitorService;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Start();
        SubscribeToLiveUpdates();
        _ = AnimateSkeletonAsync();
        await _viewModel.LoadAsync();
    }

    protected override void OnDisappearing()
    {
        _viewModel.Stop();
        UnsubscribeFromLiveUpdates();
        base.OnDisappearing();
    }

    private void SubscribeToLiveUpdates()
    {
        if (_subscribed)
        {
            return;
        }

        _leadMonitorService.LeadsUpdated += OnLeadsUpdated;
        _subscribed = true;
    }

    private void UnsubscribeFromLiveUpdates()
    {
        if (!_subscribed)
        {
            return;
        }

        _leadMonitorService.LeadsUpdated -= OnLeadsUpdated;
        _subscribed = false;
    }

    private async void OnLeadsUpdated(object? sender, EventArgs e)
    {
        if (!IsVisible)
        {
            return;
        }

        await _viewModel.LoadAsync();
    }

    private async Task AnimateSkeletonAsync()
    {
        while (IsVisible)
        {
            if (!_viewModel.IsLoading)
            {
                await Task.Delay(450);
                continue;
            }

            foreach (var frame in new[] { Skeleton1, Skeleton2, Skeleton3 })
            {
                await frame.TranslateTo(12, 0, 450, Easing.SinInOut);
                await frame.TranslateTo(0, 0, 450, Easing.SinInOut);
            }

            await Task.Delay(120);
        }
    }
}
