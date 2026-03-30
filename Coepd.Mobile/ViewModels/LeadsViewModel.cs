using System.Collections.ObjectModel;
using System.Windows.Input;
using Coepd.Mobile.Models;
using Coepd.Mobile.Services;

namespace Coepd.Mobile.ViewModels;

public class LeadsViewModel : BaseViewModel
{
    private readonly ApiSession _session;
    private readonly LeadApiService _leadApiService;
    private readonly ObservableCollection<LeadModel> _filteredLeads = new();
    private string _selectedFilter = "All";
    private string _searchQuery = string.Empty;

    public LeadsViewModel(ApiSession session, LeadApiService leadApiService)
    {
        _session = session;
        _leadApiService = leadApiService;
        Leads = new ObservableCollection<LeadModel>();
        FilteredLeads = _filteredLeads;

        SearchCommand = new Command(ApplyFilter);
        DeleteLeadCommand = new Command<LeadModel>(async lead => await DeleteLeadAsync(lead));
        CallLeadCommand = new Command<LeadModel>(async lead => await CallLeadAsync(lead));
        ApplyFilterCommand = new Command<string>(filter =>
        {
            SelectedFilter = filter ?? "All";
            OnFilterChanged();
        });
        RefreshCommand = new Command(async () => await LoadAsync());

    }

    public ObservableCollection<LeadModel> Leads { get; }
    public ObservableCollection<LeadModel> FilteredLeads { get; }

    public string SelectedFilter
    {
        get => _selectedFilter;
        set
        {
            if (SetProperty(ref _selectedFilter, value))
            {
                RaiseFilterChipState();
            }
        }
    }

    public string SearchQuery
    {
        get => _searchQuery;
        set => SetProperty(ref _searchQuery, value);
    }

    public bool IsAdmin => string.Equals(_session.CurrentRole, "admin", StringComparison.OrdinalIgnoreCase);

    public ICommand SearchCommand { get; }
    public ICommand DeleteLeadCommand { get; }
    public ICommand CallLeadCommand { get; }
    public ICommand ApplyFilterCommand { get; }
    public ICommand RefreshCommand { get; }

    public Color AllFilterBackground => GetFilterBackground("All");
    public Color TodayFilterBackground => GetFilterBackground("Today");
    public Color ChatbotFilterBackground => GetFilterBackground("Chatbot");
    public Color WebsiteFilterBackground => GetFilterBackground("Website");

    public Color AllFilterTextColor => GetFilterTextColor("All");
    public Color TodayFilterTextColor => GetFilterTextColor("Today");
    public Color ChatbotFilterTextColor => GetFilterTextColor("Chatbot");
    public Color WebsiteFilterTextColor => GetFilterTextColor("Website");

    public void Start()
    {
        // Live polling is centralized in LeadMonitorService to avoid duplicate network calls.
    }

    public void Stop()
    {
        // Live polling is centralized in LeadMonitorService to avoid duplicate network calls.
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await LoadAsync(true, true, cancellationToken);
    }

    private async Task LoadAsync(bool showLoader, CancellationToken cancellationToken = default)
    {
        await LoadAsync(showLoader, false, cancellationToken);
    }

    private async Task LoadAsync(bool showLoader, bool forceRefresh, CancellationToken cancellationToken = default)
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
            var leads = await _leadApiService.GetLeadsAsync(null, forceRefresh, cancellationToken);
            MergeLeads(leads);

            ApplyFilter();
            OnPropertyChanged(nameof(IsAdmin));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            ApplyFilter();
        }
        finally
        {
            if (showLoader)
            {
                IsLoading = false;
            }
        }
    }

    async Task DeleteLeadAsync(LeadModel? lead)
    {
        if (lead is null || !IsAdmin)
        {
            return;
        }

        await _leadApiService.DeleteLeadAsync(lead.Id);
        Leads.Remove(lead);
        FilteredLeads.Remove(lead);
    }

    static async Task CallLeadAsync(LeadModel? lead)
    {
        if (lead is null || string.IsNullOrWhiteSpace(lead.Phone))
        {
            return;
        }

        await Launcher.Default.OpenAsync($"tel:{lead.Phone}");
    }

    void OnFilterChanged()
    {
        ApplyFilter();
    }

    void ApplyFilter()
    {
        IEnumerable<LeadModel> query = Leads.OrderByDescending(x => x.CreatedAt);

        query = SelectedFilter switch
        {
            "Today" => query.Where(x => x.CreatedAt.ToLocalTime().Date == DateTime.Now.Date),
            "Chatbot" => query.Where(x => x.Source == LeadSource.Chatbot),
            "Website" => query.Where(x => x.Source == LeadSource.Website),
            _ => query
        };

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var search = SearchQuery.Trim().ToLowerInvariant();
            query = query.Where(x =>
                (x.Name ?? string.Empty).ToLowerInvariant().Contains(search) ||
                (x.Phone ?? string.Empty).ToLowerInvariant().Contains(search) ||
                (x.Email ?? string.Empty).ToLowerInvariant().Contains(search) ||
                (x.Location ?? string.Empty).ToLowerInvariant().Contains(search));
        }

        FilteredLeads.Clear();
        foreach (var lead in query)
        {
            FilteredLeads.Add(lead);
        }

        OnPropertyChanged(nameof(FilteredLeads));
    }

    Color GetFilterBackground(string filter) => SelectedFilter == filter
        ? Color.FromArgb("#3B82F6")
        : Colors.White;

    Color GetFilterTextColor(string filter) => SelectedFilter == filter
        ? Colors.White
        : Color.FromArgb("#6B7280");

    void RaiseFilterChipState()
    {
        OnPropertyChanged(nameof(AllFilterBackground));
        OnPropertyChanged(nameof(TodayFilterBackground));
        OnPropertyChanged(nameof(ChatbotFilterBackground));
        OnPropertyChanged(nameof(WebsiteFilterBackground));
        OnPropertyChanged(nameof(AllFilterTextColor));
        OnPropertyChanged(nameof(TodayFilterTextColor));
        OnPropertyChanged(nameof(ChatbotFilterTextColor));
        OnPropertyChanged(nameof(WebsiteFilterTextColor));
    }

    void MergeLeads(IEnumerable<LeadModel> latestLeads)
    {
        var latest = latestLeads
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        var latestIds = latest.Select(x => x.Id).ToHashSet();

        for (var i = Leads.Count - 1; i >= 0; i--)
        {
            if (!latestIds.Contains(Leads[i].Id))
            {
                Leads.RemoveAt(i);
            }
        }

        foreach (var lead in latest)
        {
            var existing = Leads.FirstOrDefault(x => x.Id == lead.Id);
            if (existing == null)
            {
                Leads.Add(lead);
                continue;
            }

            existing.Name = lead.Name;
            existing.Phone = lead.Phone;
            existing.Email = lead.Email;
            existing.Location = lead.Location;
            existing.Domain = lead.Domain;
            existing.WhatsAppConsent = lead.WhatsAppConsent;
            existing.Source = lead.Source;
            existing.CreatedAt = lead.CreatedAt;
        }

        var ordered = Leads.OrderByDescending(x => x.CreatedAt).ToList();
        Leads.Clear();
        foreach (var lead in ordered)
        {
            Leads.Add(lead);
        }
    }
}
