using System.Net;
using System.Text.Json;

namespace Coepd.Mobile.Services;

public class ApiSession
{
    private CookieContainer _cookies = new();
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private HttpClient _client;
    private string _baseUrl;

    public ApiSession()
    {
        _baseUrl = Preferences.Default.Get("base_url", "https://coepdfinishingschool.somee.com/");
        _client = CreateClient(_baseUrl);
    }

    public JsonSerializerOptions JsonOptions => _jsonOptions;

    public string BaseUrl
    {
        get => _baseUrl;
        set
        {
            _baseUrl = NormalizeBaseUrl(value);
            Preferences.Default.Set("base_url", _baseUrl);
            _client = CreateClient(_baseUrl);
        }
    }

    public string CurrentRole
    {
        get => Preferences.Default.Get("current_role", string.Empty);
        set => Preferences.Default.Set("current_role", value ?? string.Empty);
    }

    public string CurrentEmail
    {
        get => Preferences.Default.Get("current_email", string.Empty);
        set => Preferences.Default.Set("current_email", value ?? string.Empty);
    }

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(CurrentRole);

    public HttpClient Client => _client;

    public void Clear()
    {
        _cookies = new CookieContainer();
        CurrentRole = string.Empty;
        CurrentEmail = string.Empty;
        _client = CreateClient(_baseUrl);
    }

    private HttpClient CreateClient(string baseUrl)
    {
        var handler = new HttpClientHandler
        {
            CookieContainer = _cookies,
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
}
