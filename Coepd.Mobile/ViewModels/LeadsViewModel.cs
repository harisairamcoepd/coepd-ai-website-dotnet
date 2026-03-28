using System.Collections.ObjectModel;
using Coepd.Mobile.Models;
using Coepd.Mobile.Services;

namespace Coepd.Mobile.ViewModels;

public class LeadsViewModel : BaseViewModel
{
    private readonly ApiSession _session;
    private readonly LeadApiService _leadApiService;
    private string _searchText = string.Empty;
    private bool _isRefreshing;

    public LeadsViewModel(ApiSession session, LeadApiService leadApiService)
    {
        _session = session;
        _leadApiService = leadApiService;
        Title = "Lead Explorer";
        Leads = new ObservableCollection<Lead>();
    }

    public ObservableCollection<Lead> Leads { get; }

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public bool IsRefreshing
    {
        get => _isRefreshing;
        set => SetProperty(ref _isRefreshing, value);
    }

    public bool IsAdmin => string.Equals(_session.CurrentRole, "admin", StringComparison.OrdinalIgnoreCase);
    public string RoleLabel => IsAdmin ? "Admin access" : "Staff access";
    public string EmptyStateTitle => IsBusy ? "Syncing leads..." : "No leads found";
    public string EmptyStateText => IsBusy ? "Please wait while we contact the live website." : "Pull to refresh or search again.";
    public int LeadCount => Leads.Count;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var leads = await _leadApiService.GetLeadsAsync(SearchText, cancellationToken);
            Leads.Clear();
            foreach (var lead in leads)
            {
                Leads.Add(lead);
            }

            StatusMessage = Leads.Count == 0 ? "No leads found." : $"Loaded {Leads.Count} live leads.";
            RaisePropertyChanged(nameof(LeadCount));
            RaisePropertyChanged(nameof(EmptyStateTitle));
            RaisePropertyChanged(nameof(EmptyStateText));
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            RaisePropertyChanged(nameof(EmptyStateTitle));
            RaisePropertyChanged(nameof(EmptyStateText));
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    public async Task DeleteLeadAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        if (lead == null || !IsAdmin) return;

        await _leadApiService.DeleteLeadAsync(lead.Id, cancellationToken);
        Leads.Remove(lead);
        StatusMessage = "Lead deleted successfully.";
        RaisePropertyChanged(nameof(LeadCount));
    }
}
