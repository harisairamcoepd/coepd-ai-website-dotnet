using Coepd.Mobile.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Coepd.Mobile;

public partial class AppShell : Shell
{
    public AppShell(IServiceProvider serviceProvider)
    {
        InitializeComponent();

        Items.Clear();

        var tabBar = new TabBar();
        tabBar.Items.Add(new ShellContent
        {
            Title = "Dashboard",
            Route = "dashboard",
            Icon = "dotnet_bot.png",
            ContentTemplate = new DataTemplate(() => serviceProvider.GetRequiredService<DashboardPage>())
        });
        tabBar.Items.Add(new ShellContent
        {
            Title = "Leads",
            Route = "leads",
            Icon = "dotnet_bot.png",
            ContentTemplate = new DataTemplate(() => serviceProvider.GetRequiredService<LeadsPage>())
        });

        Items.Add(tabBar);
    }
}
