using System.Text.Json.Serialization;

namespace Coepd.Mobile.Models;

public sealed class LoginRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

public sealed class LoginResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
}

public sealed class LeadsEnvelope
{
    [JsonPropertyName("leads")]
    public List<LeadDto> Leads { get; set; } = new();
}

public sealed class LeadDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public string CreatedAtRaw { get; set; } = string.Empty;

    [JsonPropertyName("datetime_display")]
    public string DateTimeDisplay { get; set; } = string.Empty;
}

public sealed class StatsDto
{
    [JsonPropertyName("total_leads")]
    public int TotalLeads { get; set; }

    [JsonPropertyName("today_leads")]
    public int TodayLeads { get; set; }

    [JsonPropertyName("chatbot_leads")]
    public int ChatbotLeads { get; set; }

    [JsonPropertyName("website_leads")]
    public int WebsiteLeads { get; set; }
}
