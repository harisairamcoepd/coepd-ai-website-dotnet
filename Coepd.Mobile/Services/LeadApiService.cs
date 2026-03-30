using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Text.Json;
using Coepd.Mobile.Models;

namespace Coepd.Mobile.Services;

public class LeadApiService
{
    private readonly ApiSession _session;

    public LeadApiService(ApiSession session)
    {
        _session = session;
    }

    public async Task<DashboardStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        if (string.Equals(_session.CurrentRole, "staff", StringComparison.OrdinalIgnoreCase))
        {
            return await BuildStatsFromLeadsAsync(cancellationToken);
        }

        var response = await _session.Client.GetAsync("api/admin/stats", cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode || LooksLikeHtml(content))
        {
            return await BuildStatsFromLeadsAsync(cancellationToken);
        }

        var payload = JsonSerializer.Deserialize<StatsDto>(content, _session.JsonOptions);
        if (payload == null)
        {
            return await BuildStatsFromLeadsAsync(cancellationToken);
        }

        return new DashboardStats
        {
            TotalLeads = payload?.TotalLeads ?? 0,
            TodayLeads = payload?.TodayLeads ?? 0,
            ChatbotLeads = payload?.ChatbotLeads ?? 0,
            WebsiteLeads = payload?.WebsiteLeads ?? 0
        };
    }

    public async Task<List<LeadModel>> GetLeadsAsync(string? search = null, CancellationToken cancellationToken = default)
    {
        var url = "api/admin/leads";
        if (!string.IsNullOrWhiteSpace(search))
        {
            url += "?search=" + WebUtility.UrlEncode(search.Trim());
        }

        var response = await _session.Client.GetAsync(url, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode || LooksLikeHtml(content))
        {
            throw new InvalidOperationException("Unable to load leads from server.");
        }

        var leads = TryReadEnvelope(content) ?? TryReadDirectList(content) ?? new List<LeadDto>();
        var isAdmin = string.Equals(_session.CurrentRole, "admin", StringComparison.OrdinalIgnoreCase);
        return leads.Select(x => MapLead(x, isAdmin)).ToList();
    }

    public async Task DeleteLeadAsync(int leadId, CancellationToken cancellationToken = default)
    {
        var response = await _session.Client.DeleteAsync("api/admin/leads/" + leadId.ToString(), cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Unable to delete lead.");
        }
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

    private static LeadModel MapLead(LeadDto dto, bool canDelete)
    {
        var createdAt = ParseServerDate(dto.CreatedAtRaw);
        _ = canDelete;

        return new LeadModel
        {
            Id = dto.Id,
            Name = dto.Name,
            Phone = dto.Phone,
            Email = dto.Email,
            Location = dto.Location,
            Source = MapSource(dto.Source),
            CreatedAt = createdAt
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

    private async Task<DashboardStats> BuildStatsFromLeadsAsync(CancellationToken cancellationToken)
    {
        var leads = await GetLeadsAsync(null, cancellationToken);
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

    private static DateTime ParseServerDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DateTime.UtcNow;
        }

        if (DateTime.TryParse(value, out var parsed))
        {
            return parsed.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(parsed, DateTimeKind.Local).ToUniversalTime() : parsed.ToUniversalTime();
        }

        var match = Regex.Match(value, @"\/Date\(([-]?\d+)\)\/");
        if (match.Success && long.TryParse(match.Groups[1].Value, out var milliseconds))
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime;
        }

        return DateTime.UtcNow;
    }
}
