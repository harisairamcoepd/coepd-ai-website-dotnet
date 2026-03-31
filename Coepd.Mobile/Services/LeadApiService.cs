using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Coepd.Mobile.Models;

namespace Coepd.Mobile.Services;

public class LeadApiService
{
    private static readonly TimeSpan CacheWindow = TimeSpan.FromSeconds(3);
    private readonly ApiSession _session;
    private readonly SemaphoreSlim _cacheGate = new(1, 1);
    private DateTime _lastLeadsFetchUtc = DateTime.MinValue;
    private DateTime _lastStatsFetchUtc = DateTime.MinValue;
    private List<LeadModel> _cachedLeads = new();
    private DashboardStats _cachedStats = new();

    public LeadApiService(ApiSession session)
    {
        _session = session;
    }

    public async Task<DashboardStats> GetStatsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        await _cacheGate.WaitAsync(cancellationToken);
        try
        {
            if (!forceRefresh && DateTime.UtcNow - _lastStatsFetchUtc < CacheWindow)
            {
                return _cachedStats;
            }
        }
        finally
        {
            _cacheGate.Release();
        }

        DashboardStats stats;
        if (string.Equals(_session.CurrentRole, "staff", StringComparison.OrdinalIgnoreCase))
        {
            stats = await BuildStatsFromLeadsAsync(forceRefresh, cancellationToken);
        }
        else
        {
            var response = await ExecuteWithRetryAsync(
                ct => _session.Client.GetAsync("api/admin/stats", ct),
                cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode || LooksLikeHtml(content))
            {
                stats = await BuildStatsFromLeadsAsync(forceRefresh, cancellationToken);
            }
            else
            {
                var payload = JsonSerializer.Deserialize<StatsDto>(content, _session.JsonOptions);
                stats = payload == null
                    ? await BuildStatsFromLeadsAsync(forceRefresh, cancellationToken)
                    : new DashboardStats
                    {
                        TotalLeads = payload.TotalLeads,
                        TodayLeads = payload.TodayLeads,
                        ChatbotLeads = payload.ChatbotLeads,
                        WebsiteLeads = payload.WebsiteLeads
                    };
            }
        }

        await _cacheGate.WaitAsync(cancellationToken);
        try
        {
            _cachedStats = stats;
            _lastStatsFetchUtc = DateTime.UtcNow;
            return _cachedStats;
        }
        finally
        {
            _cacheGate.Release();
        }
    }

    public async Task<List<LeadModel>> GetLeadsAsync(string? search = null, bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        var canUseCache = string.IsNullOrWhiteSpace(search);
        await _cacheGate.WaitAsync(cancellationToken);
        try
        {
            if (canUseCache && !forceRefresh && DateTime.UtcNow - _lastLeadsFetchUtc < CacheWindow)
            {
                return _cachedLeads.ToList();
            }
        }
        finally
        {
            _cacheGate.Release();
        }

        var leads = await GetLeadsFromServerAsync(search, cancellationToken);

        await _cacheGate.WaitAsync(cancellationToken);
        try
        {
            if (canUseCache)
            {
                _cachedLeads = leads;
                _lastLeadsFetchUtc = DateTime.UtcNow;
            }

            return leads.ToList();
        }
        finally
        {
            _cacheGate.Release();
        }
    }

    public async Task DeleteLeadAsync(int leadId, CancellationToken cancellationToken = default)
    {
        var response = await ExecuteWithRetryAsync(
            ct => _session.Client.DeleteAsync("api/admin/leads/" + leadId.ToString(), ct),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(ExtractApiError(body, response.StatusCode, "Unable to delete lead."));
        }

        await _cacheGate.WaitAsync(cancellationToken);
        try
        {
            _cachedLeads.RemoveAll(x => x.Id == leadId);
            _lastLeadsFetchUtc = DateTime.MinValue;
            _lastStatsFetchUtc = DateTime.MinValue;
        }
        finally
        {
            _cacheGate.Release();
        }
    }

    private async Task<List<LeadModel>> GetLeadsFromServerAsync(string? search, CancellationToken cancellationToken)
    {
        var query = string.IsNullOrWhiteSpace(search) ? string.Empty : "?search=" + WebUtility.UrlEncode(search.Trim());
        var candidates = string.Equals(_session.CurrentRole, "staff", StringComparison.OrdinalIgnoreCase)
            ? new[] { "api/staff/leads" + query, "api/admin/leads" + query }
            : new[] { "api/admin/leads" + query, "api/staff/leads" + query };

        foreach (var url in candidates)
        {
            HttpResponseMessage response;
            try
            {
                response = await ExecuteWithRetryAsync(ct => _session.Client.GetAsync(url, ct), cancellationToken);
            }
            catch
            {
                continue;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new InvalidOperationException("Session expired. Please login again.");
            }

            if (!response.IsSuccessStatusCode || LooksLikeHtml(content))
            {
                continue;
            }

            var dtoLeads = TryReadEnvelope(content) ?? TryReadDirectList(content) ?? new List<LeadDto>();
            return dtoLeads.Select(MapLead).OrderByDescending(x => x.CreatedAt).ToList();
        }

        throw new InvalidOperationException("Unable to load leads from server.");
    }

    private static LeadModel MapLead(LeadDto dto)
    {
        return new LeadModel
        {
            Id = dto.Id,
            Name = dto.Name ?? string.Empty,
            Phone = dto.Phone ?? string.Empty,
            Email = dto.Email ?? string.Empty,
            Location = dto.Location ?? string.Empty,
            Domain = dto.Domain ?? string.Empty,
            WhatsAppConsent = dto.WhatsappConsent ?? false,
            Source = MapSource(dto.Source),
            CreatedAt = ParseServerDate(dto.CreatedAtRaw)
        };
    }

    private static LeadSource MapSource(string? source)
    {
        if (string.Equals(source, "chatbot", StringComparison.OrdinalIgnoreCase))
        {
            return LeadSource.Chatbot;
        }

        return LeadSource.Website;
    }

    private async Task<DashboardStats> BuildStatsFromLeadsAsync(bool forceRefresh, CancellationToken cancellationToken)
    {
        var leads = await GetLeadsAsync(null, forceRefresh, cancellationToken);
        var today = DateTime.Now.Date;
        return new DashboardStats
        {
            TotalLeads = leads.Count,
            TodayLeads = leads.Count(x => x.CreatedAt.ToLocalTime().Date == today),
            ChatbotLeads = leads.Count(x => x.Source == LeadSource.Chatbot),
            WebsiteLeads = leads.Count(x => x.Source == LeadSource.Website)
        };
    }

    private static bool LooksLikeHtml(string content)
    {
        return !string.IsNullOrWhiteSpace(content) &&
               (content.Contains("<html", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("<!DOCTYPE", StringComparison.OrdinalIgnoreCase));
    }

    private List<LeadDto>? TryReadEnvelope(string json)
    {
        var payload = JsonSerializer.Deserialize<LeadsEnvelope>(json, _session.JsonOptions);
        return payload?.Leads;
    }

    private List<LeadDto>? TryReadDirectList(string json)
    {
        return JsonSerializer.Deserialize<List<LeadDto>>(json, _session.JsonOptions);
    }

    private static DateTime ParseServerDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DateTime.UtcNow;
        }

        if (DateTime.TryParse(value, out var parsed))
        {
            return parsed.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(parsed, DateTimeKind.Local).ToUniversalTime()
                : parsed.ToUniversalTime();
        }

        var match = Regex.Match(value, @"\/Date\(([-]?\d+)\)\/");
        if (match.Success && long.TryParse(match.Groups[1].Value, out var milliseconds))
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime;
        }

        return DateTime.UtcNow;
    }

    private static async Task<HttpResponseMessage> ExecuteWithRetryAsync(
        Func<CancellationToken, Task<HttpResponseMessage>> request,
        CancellationToken cancellationToken)
    {
        Exception? lastError = null;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                var response = await request(cancellationToken);
                if ((int)response.StatusCode >= 500 && attempt < 3)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(220 * attempt), cancellationToken);
                    continue;
                }

                return response;
            }
            catch (Exception ex) when (attempt < 3)
            {
                lastError = ex;
                await Task.Delay(TimeSpan.FromMilliseconds(220 * attempt), cancellationToken);
            }
            catch (Exception ex)
            {
                lastError = ex;
                break;
            }
        }

        throw new InvalidOperationException("Network request failed after retries. Check connectivity and server status.", lastError);
    }

    private static string ExtractApiError(string body, HttpStatusCode statusCode, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("error", out var errorElement))
                {
                    var err = errorElement.GetString();
                    if (!string.IsNullOrWhiteSpace(err))
                    {
                        return err;
                    }
                }
            }
            catch
            {
                // Keep fallback.
            }
        }

        if ((int)statusCode == 422)
        {
            return "Invalid request. Please check lead details and try again.";
        }

        return fallback;
    }
}
