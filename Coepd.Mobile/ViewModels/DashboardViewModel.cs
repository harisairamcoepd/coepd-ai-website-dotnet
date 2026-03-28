using Coepd.Mobile.Models;
using Coepd.Mobile.Services;

namespace Coepd.Mobile.ViewModels;

public class DashboardViewModel : BaseViewModel
{
    private readonly ApiSession _session;
    private readonly LeadApiService _leadApiService;
    private DashboardStats _stats = new();

    public DashboardViewModel(ApiSession session, LeadApiService leadApiService)
    {
        _session = session;
        _leadApiService = leadApiService;
        Title = "Dashboard";
    }

    public string RoleLabel => string.IsNullOrWhiteSpace(_session.CurrentRole) ? "Staff" : _session.CurrentRole.ToUpperInvariant();
    public bool IsAdmin => string.Equals(_session.CurrentRole, "admin", StringComparison.OrdinalIgnoreCase);
    public int TotalLeads => _stats.TotalLeads;
    public int TodayLeads => _stats.TodayLeads;
    public int ChatbotLeads => _stats.ChatbotLeads;
    public int WebsiteLeads => _stats.WebsiteLeads;
    public string WorkspaceLabel => IsAdmin ? "Full control workspace" : "Read-only lead workspace";
    public string SummaryText => _stats.SummaryText;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            _stats = await _leadApiService.GetStatsAsync(cancellationToken);
            OnStatsChanged();
            StatusMessage = "Dashboard synced with live website.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnStatsChanged()
    {
        RaisePropertyChanged(nameof(RoleLabel));
        RaisePropertyChanged(nameof(IsAdmin));
        RaisePropertyChanged(nameof(WorkspaceLabel));
        RaisePropertyChanged(nameof(TotalLeads));
        RaisePropertyChanged(nameof(TodayLeads));
        RaisePropertyChanged(nameof(ChatbotLeads));
        RaisePropertyChanged(nameof(WebsiteLeads));
        RaisePropertyChanged(nameof(SummaryText));
    }
}
