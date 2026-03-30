using System.Net.Http.Json;
using System.Text.Json;
using Coepd.Mobile.Models;

namespace Coepd.Mobile.Services;

public class AuthService
{
    private const string SavedPasswordKey = "saved_password";
    private const string SessionPersistedKey = "session_persisted";
    private readonly ApiSession _session;
    private readonly LeadMonitorService _leadMonitorService;

    public AuthService(ApiSession session, LeadMonitorService leadMonitorService)
    {
        _session = session;
        _leadMonitorService = leadMonitorService;
    }

    public async Task<LoginResponse> LoginAsync(User user, CancellationToken cancellationToken = default)
    {
        var role = (user.Role ?? "staff").Trim().ToLowerInvariant();
        var endpoint = role == "admin" ? "api/admin/login" : "api/staff/login";
        var normalizedEmail = (user.Email ?? string.Empty).Trim();
        var response = await PostLoginWithRetryAsync(endpoint, normalizedEmail, user.Password ?? string.Empty, cancellationToken);

        var payload = await TryReadLoginResponseAsync(response, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new LoginResponse
            {
                Success = false,
                Error = payload?.Error ?? $"Login failed ({(int)response.StatusCode}). Check credentials and server URL."
            };
        }

        var result = payload ?? new LoginResponse { Success = false, Error = "Unable to read login response." };
        if (result.Success)
        {
            var confirmedRole = await ConfirmSessionRoleAsync(cancellationToken) ?? (string.IsNullOrWhiteSpace(result.Role) ? role : result.Role);
            _session.CurrentRole = confirmedRole;
            _session.CurrentEmail = normalizedEmail;
            await SaveCredentialsAsync(normalizedEmail, user.Password ?? string.Empty);
            Preferences.Default.Set(SessionPersistedKey, true);
            await _leadMonitorService.StartAsync();
        }

        return result;
    }

    public async Task<bool> TryRestoreSessionAsync(CancellationToken cancellationToken = default)
    {
        if (!Preferences.Default.Get(SessionPersistedKey, false))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(_session.CurrentEmail) || string.IsNullOrWhiteSpace(_session.CurrentRole))
        {
            return false;
        }

        try
        {
            var savedPassword = await SecureStorage.Default.GetAsync(SavedPasswordKey);
            if (string.IsNullOrWhiteSpace(savedPassword))
            {
                return false;
            }

            var result = await LoginAsync(new User
            {
                Email = _session.CurrentEmail,
                Password = savedPassword,
                Role = _session.CurrentRole
            }, cancellationToken);

            return result.Success;
        }
        catch
        {
            return false;
        }
    }

    public void Logout()
    {
        _leadMonitorService.Stop();
        SecureStorage.Default.Remove(SavedPasswordKey);
        Preferences.Default.Set(SessionPersistedKey, false);
        _session.Clear();
    }

    private static async Task SaveCredentialsAsync(string email, string password)
    {
        await SecureStorage.Default.SetAsync(SavedPasswordKey, password ?? string.Empty);
    }

    private async Task<string?> ConfirmSessionRoleAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _session.Client.GetAsync("auth/me", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(json) || json.Contains("<html", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("user", out var userElement))
            {
                return null;
            }

            if (!userElement.TryGetProperty("role", out var roleElement))
            {
                return null;
            }

            return roleElement.GetString();
        }
        catch
        {
            return null;
        }
    }

    private async Task<HttpResponseMessage> PostLoginWithRetryAsync(
        string endpoint,
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                var response = await _session.Client.PostAsJsonAsync(endpoint, new LoginRequest
                {
                    Email = email,
                    Password = password
                }, cancellationToken);

                if ((int)response.StatusCode >= 500 && attempt < 3)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt), cancellationToken);
                    continue;
                }

                return response;
            }
            catch when (attempt < 3)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt), cancellationToken);
            }
        }

        throw new InvalidOperationException("Unable to connect to login service.");
    }

    private async Task<LoginResponse?> TryReadLoginResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
        if (!contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            return await response.Content.ReadFromJsonAsync<LoginResponse>(_session.JsonOptions, cancellationToken);
        }
        catch
        {
            return null;
        }
    }
}
