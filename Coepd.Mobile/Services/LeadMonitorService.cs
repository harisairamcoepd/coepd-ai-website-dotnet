using Coepd.Mobile.Models;

namespace Coepd.Mobile.Services;

public class LeadMonitorService
{
    private const string LastSeenLeadIdKey = "lead_monitor_last_seen_id";
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private readonly ApiSession _session;
    private readonly LeadApiService _leadApiService;
    private readonly NotificationService _notificationService;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;
    private Task? _worker;
    private bool _initialized;

    public event EventHandler? LeadsUpdated;
    public event EventHandler<LeadModel>? NewLeadDetected;

    public LeadMonitorService(ApiSession session, LeadApiService leadApiService, NotificationService notificationService)
    {
        _session = session;
        _leadApiService = leadApiService;
        _notificationService = notificationService;
    }

    public async Task StartAsync()
    {
        await _notificationService.EnsurePermissionAsync();
        if (_worker != null || !_session.IsAuthenticated)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _timer = new PeriodicTimer(PollInterval);
        _worker = Task.Run(() => RunAsync(_cts.Token));
    }

    public void Stop()
    {
        _cts?.Cancel();
        _timer?.Dispose();
        _cts = null;
        _timer = null;
        _worker = null;
        _initialized = false;
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        await CheckForNewLeadsAsync(cancellationToken);

        while (!cancellationToken.IsCancellationRequested && _timer != null)
        {
            try
            {
                if (!await _timer.WaitForNextTickAsync(cancellationToken))
                {
                    break;
                }

                await CheckForNewLeadsAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Keep polling even if a single request fails.
            }
        }
    }

    private async Task CheckForNewLeadsAsync(CancellationToken cancellationToken)
    {
        if (!_session.IsAuthenticated)
        {
            return;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var leads = await _leadApiService.GetLeadsAsync(null, cancellationToken);
            var newestLead = leads
                .OrderByDescending(x => x.Id)
                .ThenByDescending(x => x.CreatedAt)
                .FirstOrDefault();

            if (newestLead == null)
            {
                return;
            }

            var lastSeenLeadId = Preferences.Default.Get(LastSeenLeadIdKey, 0);
            if (!_initialized)
            {
                Preferences.Default.Set(LastSeenLeadIdKey, newestLead.Id);
                _initialized = true;
                MainThread.BeginInvokeOnMainThread(() => LeadsUpdated?.Invoke(this, EventArgs.Empty));
                return;
            }

            if (newestLead.Id <= lastSeenLeadId)
            {
                MainThread.BeginInvokeOnMainThread(() => LeadsUpdated?.Invoke(this, EventArgs.Empty));
                return;
            }

            Preferences.Default.Set(LastSeenLeadIdKey, newestLead.Id);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                NewLeadDetected?.Invoke(this, newestLead);
                LeadsUpdated?.Invoke(this, EventArgs.Empty);
            });
            await _notificationService.ShowNewLeadNotificationAsync(
                "New Lead Received",
                BuildLeadMessage(newestLead));
        }
        finally
        {
            _gate.Release();
        }
    }

    private static string BuildLeadMessage(LeadModel lead)
    {
        var source = string.IsNullOrWhiteSpace(lead.SourceLabel) ? "Unknown source" : lead.SourceLabel;
        return string.IsNullOrWhiteSpace(lead.Name)
            ? $"A new {source} lead was added."
            : $"{lead.Name} was added from {source}.";
    }
}
