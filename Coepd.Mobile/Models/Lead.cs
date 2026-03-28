namespace Coepd.Mobile.Models;

public class Lead
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string DateTimeDisplay { get; set; } = string.Empty;
    public bool CanDelete { get; set; }

    public string Initials
    {
        get
        {
            var parts = (Name ?? string.Empty)
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Take(2)
                .Select(x => char.ToUpperInvariant(x[0]));
            var joined = string.Concat(parts);
            return string.IsNullOrWhiteSpace(joined) ? "NA" : joined;
        }
    }

    public string SourceLabel =>
        string.IsNullOrWhiteSpace(Source)
            ? "Unknown"
            : (Source.Equals("webpage", StringComparison.OrdinalIgnoreCase) ? "Website" : Source.Trim());

    public string SourceAccent =>
        Source.Equals("chatbot", StringComparison.OrdinalIgnoreCase) ? "#0F766E" : "#2563EB";
}
