namespace Coepd.Mobile.Models;

public class LeadItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string DateTimeDisplay { get; set; } = string.Empty;

    public string SourceLabel => string.IsNullOrWhiteSpace(Source) ? "UNKNOWN" : Source.Trim().ToUpperInvariant();
}
