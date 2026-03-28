using Coepd.Mobile.Views;

namespace Coepd.Mobile;

public partial class App : Application
{
    private readonly SplashPage _splashPage;

    public App(SplashPage splashPage)
    {
        InitializeComponent();
        _splashPage = splashPage;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new NavigationPage(_splashPage));
    }
}
