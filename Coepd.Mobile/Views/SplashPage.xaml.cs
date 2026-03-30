using Coepd.Mobile.Services;

namespace Coepd.Mobile.Views;

public partial class SplashPage : ContentPage
{
    private readonly ApiSession _session;
    private readonly AuthService _authService;
    private readonly IServiceProvider _serviceProvider;
    private readonly RoleSelectionPage _roleSelectionPage;
    private readonly NotificationService _notificationService;
    private bool _navigated;

    public SplashPage(
        ApiSession session,
        AuthService authService,
        IServiceProvider serviceProvider,
        RoleSelectionPage roleSelectionPage,
        NotificationService notificationService)
    {
        InitializeComponent();
        _session = session;
        _authService = authService;
        _serviceProvider = serviceProvider;
        _roleSelectionPage = roleSelectionPage;
        _notificationService = notificationService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        SplashContent.Opacity = 0;
        SplashContent.Scale = 0.94;
        await Task.WhenAll(
            SplashContent.FadeTo(1, 350, Easing.CubicOut),
            SplashContent.ScaleTo(1, 350, Easing.CubicOut));
        await _notificationService.EnsurePermissionAsync();
        if (_navigated) return;
        _navigated = true;
        await Task.Delay(1400);
        if (_session.IsAuthenticated)
        {
            Application.Current!.Windows[0].Page = _serviceProvider.GetRequiredService<AppShell>();
            _ = RestoreSessionInBackgroundAsync();
            return;
        }

        await Navigation.PushAsync(_roleSelectionPage);
        Navigation.RemovePage(this);
    }

    private async Task RestoreSessionInBackgroundAsync()
    {
        try
        {
            await _authService.TryRestoreSessionAsync();
        }
        catch
        {
            // Keep the user signed in locally until they explicitly log out.
        }
    }
}
