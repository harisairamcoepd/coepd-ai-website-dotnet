namespace Coepd.Mobile.Models;

public enum LeadSource
{
    Chatbot,
    Website
}

public class LeadModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public LeadSource Source { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool IsNew => DateTime.UtcNow.Subtract(CreatedAt.ToUniversalTime()).TotalSeconds <= 60;

    public string Initials
    {
        get
        {
            var parts = (Name ?? string.Empty)
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Take(2)
                .Select(part => char.ToUpperInvariant(part[0]))
                .ToArray();

            return parts.Length == 0 ? "NA" : string.Concat(parts);
        }
    }

    public string SourceLabel => Source == LeadSource.Chatbot ? "CHATBOT" : "WEBSITE";
    public string TimestampText => CreatedAt.ToLocalTime().ToString("dd MMM yyyy, hh:mm tt");
    public Color SourceColor => Source == LeadSource.Chatbot
        ? Color.FromArgb("#14B8A6")
        : Color.FromArgb("#3B82F6");
}
