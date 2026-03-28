using Coepd.Mobile.Models;
using Coepd.Mobile.Services;

namespace Coepd.Mobile.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private readonly ApiSession _session;
    private readonly AuthService _authService;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _baseUrl = string.Empty;
    private string _role = "staff";

    public LoginViewModel(ApiSession session, AuthService authService)
    {
        _session = session;
        _authService = authService;
        _baseUrl = _session.BaseUrl;
        Title = "Secure Login";
        StatusMessage = "Use your COEPD credentials to continue.";
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string BaseUrl
    {
        get => _baseUrl;
        set => SetProperty(ref _baseUrl, value);
    }

    public string Role
    {
        get => _role;
        set => SetProperty(ref _role, value);
    }

    public async Task<(bool Success, string Role, string Error)> LoginAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy) return (false, string.Empty, "Please wait.");
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            return (false, string.Empty, "Enter both email and password.");
        }

        IsBusy = true;
        try
        {
            _session.BaseUrl = BaseUrl;
            var response = await _authService.LoginAsync(new User
            {
                Email = Email,
                Password = Password,
                Role = Role
            }, cancellationToken);

            if (!response.Success)
            {
                StatusMessage = string.IsNullOrWhiteSpace(response.Error) ? "Login failed." : response.Error;
                return (false, string.Empty, StatusMessage);
            }

            var resolvedRole = string.IsNullOrWhiteSpace(response.Role) ? Role : response.Role;
            StatusMessage = "Login successful.";
            return (true, resolvedRole, string.Empty);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
