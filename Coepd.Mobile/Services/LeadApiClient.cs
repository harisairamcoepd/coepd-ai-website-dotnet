using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Coepd.Mobile.Models;

namespace Coepd.Mobile.Services;

public class LeadApiClient
{
    private readonly CookieContainer _cookieContainer = new();
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private HttpClient _httpClient;

    public LeadApiClient()
    {
        _httpClient = CreateHttpClient(BaseUrl);
    }

    public string BaseUrl
    {
        get => Preferences.Default.Get(nameof(BaseUrl), "https://coepdfinishingschool.somee.com/");
        set
        {
            Preferences.Default.Set(nameof(BaseUrl), NormalizeBaseUrl(value));
            _httpClient = CreateHttpClient(Preferences.Default.Get(nameof(BaseUrl), "https://coepdfinishingschool.somee.com/"));
        }
    }

    public async Task<LoginResponse> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "Auth/ApiLogin",
            new LoginRequest { Email = email?.Trim() ?? string.Empty, Password = password ?? string.Empty },
            cancellationToken);

        var model = await response.Content.ReadFromJsonAsync<LoginResponse>(_jsonOptions, cancellationToken);
        return model ?? new LoginResponse { Error = "Unable to read login response." };
    }

    public async Task<List<LeadItem>> GetLeadsAsync(string search, CancellationToken cancellationToken = default)
    {
        var path = string.IsNullOrWhiteSpace(search)
            ? "AdminApi/Leads?source=all"
            : "AdminApi/Leads?source=all&search=" + Uri.EscapeDataString(search.Trim());

        var response = await _httpClient.GetAsync(path, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Unable to load leads. Check login and website URL.");
        }

        var payload = await response.Content.ReadFromJsonAsync<LeadsEnvelope>(_jsonOptions, cancellationToken);
        return payload?.Leads?.Select(MapLead).ToList() ?? new List<LeadItem>();
    }

    private HttpClient CreateHttpClient(string baseUrl)
    {
        var handler = new HttpClientHandler
        {
            CookieContainer = _cookieContainer,
            UseCookies = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };

        return new HttpClient(handler)
        {
            BaseAddress = new Uri(NormalizeBaseUrl(baseUrl)),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    private static string NormalizeBaseUrl(string value)
    {
        var candidate = string.IsNullOrWhiteSpace(value) ? "https://coepdfinishingschool.somee.com/" : value.Trim();
        if (!candidate.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !candidate.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            candidate = "https://" + candidate;
        }

        return candidate.EndsWith("/") ? candidate : candidate + "/";
    }

    private static LeadItem MapLead(LeadDto dto)
    {
        return new LeadItem
        {
            Id = dto.Id,
            Name = dto.Name ?? string.Empty,
            Phone = dto.Phone ?? string.Empty,
            Email = dto.Email ?? string.Empty,
            Location = dto.Location ?? string.Empty,
            Source = dto.Source ?? string.Empty,
            CreatedAt = dto.CreatedAt,
            DateTimeDisplay = string.IsNullOrWhiteSpace(dto.DateTimeDisplay)
                ? dto.CreatedAt.ToString("dd MMM yyyy hh:mm tt")
                : dto.DateTimeDisplay
        };
    }
}
