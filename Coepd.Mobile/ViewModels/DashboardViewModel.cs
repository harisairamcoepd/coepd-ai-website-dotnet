using System.Windows.Input;
using Coepd.Mobile.Models;
using Coepd.Mobile.Services;

namespace Coepd.Mobile.ViewModels;

public class DashboardViewModel : BaseViewModel
{
    private readonly ApiSession _session;
    private readonly LeadApiService _leadApiService;
    private readonly IDispatcherTimer _timer;
    private DashboardStats _stats = new();

    public DashboardViewModel(ApiSession session, LeadApiService leadApiService)
    {
        _session = session;
        _leadApiService = leadApiService;

        RefreshCommand = new Command(async () => await LoadAsync());
        OpenLeadsCommand = new Command(async () => await Shell.Current.GoToAsync("//leads"));
        LogoutCommand = new Command(() => { });

        _timer = Application.Current!.Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(5);
        _timer.Tick += async (_, _) => await LoadAsync(false);
    }

    public int TotalLeads => _stats.TotalLeads;
    public int TodayLeads => _stats.TodayLeads;
    public int ChatbotLeads => _stats.ChatbotLeads;
    public int WebsiteLeads => _stats.WebsiteLeads;
    public bool IsAdmin => string.Equals(_session.CurrentRole, "admin", StringComparison.OrdinalIgnoreCase);
    public string RoleBadgeText => IsAdmin ? "ADMIN" : "STAFF";
    public string RoleSubtitle => IsAdmin
        ? "You have full command over lead routing, deletion, and live workspace controls."
        : "You are monitoring the live lead pipeline with staff workspace access.";
    public string TotalTrendText => "↑ 12%";
    public Color TotalTrendColor => Color.FromArgb("#22C55E");
    public string TodayTrendText => "↑ 18%";
    public Color TodayTrendColor => Color.FromArgb("#22C55E");
    public string ChatbotTrendText => "↑ 7%";
    public Color ChatbotTrendColor => Color.FromArgb("#22C55E");
    public string WebsiteTrendText => "↓ 3%";
    public Color WebsiteTrendColor => Color.FromArgb("#EF4444");

    public ICommand RefreshCommand { get; }
    public ICommand OpenLeadsCommand { get; }
    public ICommand LogoutCommand { get; set; }

    public void Start()
    {
        if (!_timer.IsRunning)
        {
            _timer.Start();
        }
    }

    public void Stop()
    {
        if (_timer.IsRunning)
        {
            _timer.Stop();
        }
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await LoadAsync(true, cancellationToken);
    }

    private async Task LoadAsync(bool showLoader, CancellationToken cancellationToken = default)
    {
        if (IsLoading)
        {
            return;
        }

        if (showLoader)
        {
            IsLoading = true;
        }

        try
        {
            _stats = await _leadApiService.GetStatsAsync(cancellationToken);
            RaiseStatsChanged();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            if (showLoader)
            {
                IsLoading = false;
            }
        }
    }

    private void RaiseStatsChanged()
    {
        OnPropertyChanged(nameof(TotalLeads));
        OnPropertyChanged(nameof(TodayLeads));
        OnPropertyChanged(nameof(ChatbotLeads));
        OnPropertyChanged(nameof(WebsiteLeads));
        OnPropertyChanged(nameof(RoleBadgeText));
        OnPropertyChanged(nameof(RoleSubtitle));
        OnPropertyChanged(nameof(IsAdmin));
    }
}
