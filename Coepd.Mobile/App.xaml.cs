using Coepd.Mobile.Views;
using Coepd.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Coepd.Mobile;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;
    private bool _fatalErrorShown;

    public App(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var splashPage = _serviceProvider.GetRequiredService<SplashPage>();
        return new Window(new NavigationPage(splashPage));
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[FATAL] Unhandled exception: {e.ExceptionObject}");
        if (Current is App app)
        {
            app.ShowFatalError("A critical error occurred. Please restart the app.");
        }
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[TASK] Unobserved exception: {e.Exception}");
        if (Current is App app)
        {
            app.ShowFatalError("A background task failed. Some data may be outdated.");
        }
        e.SetObserved();
    }

    private void ShowFatalError(string message)
    {
        if (_fatalErrorShown)
        {
            return;
        }

        _fatalErrorShown = true;
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                if (Current?.Windows.FirstOrDefault()?.Page is Page page)
                {
                    await page.DisplayAlert("COEPD Lead Command Center", message, "OK");
                }
            }
            catch
            {
                // Ignore UI alert failures in global exception path.
            }
            finally
            {
                _fatalErrorShown = false;
            }
        });
    }
}
