using System.Net.Http.Json;
using System.Text.Json;
using Coepd.Mobile.Models;

namespace Coepd.Mobile.Services;

public class AuthService
{
    private readonly ApiSession _session;

    public AuthService(ApiSession session)
    {
        _session = session;
    }

    public async Task<LoginResponse> LoginAsync(User user, CancellationToken cancellationToken = default)
    {
        var role = (user.Role ?? "staff").Trim().ToLowerInvariant();
        var endpoint = role == "admin" ? "api/admin/login" : "api/staff/login";

        var response = await _session.Client.PostAsJsonAsync(endpoint, new LoginRequest
        {
            Email = user.Email.Trim(),
            Password = user.Password
        }, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>(_session.JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new LoginResponse
            {
                Success = false,
                Error = payload?.Error ?? "Login failed. Check credentials and server URL."
            };
        }

        var result = payload ?? new LoginResponse { Success = false, Error = "Unable to read login response." };
        if (result.Success)
        {
            var confirmedRole = await ConfirmSessionRoleAsync(cancellationToken) ?? (string.IsNullOrWhiteSpace(result.Role) ? role : result.Role);
            _session.CurrentRole = confirmedRole;
            _session.CurrentEmail = user.Email.Trim();
        }

        return result;
    }

    public void Logout()
    {
        _session.Clear();
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
}
