namespace Coepd.Mobile.Views;

public partial class SplashPage : ContentPage
{
    private readonly RoleSelectionPage _roleSelectionPage;
    private bool _navigated;

    public SplashPage(RoleSelectionPage roleSelectionPage)
    {
        InitializeComponent();
        _roleSelectionPage = roleSelectionPage;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_navigated) return;
        _navigated = true;
        await Task.Delay(1400);
        await Navigation.PushAsync(_roleSelectionPage);
        Navigation.RemovePage(this);
    }
}
