using System.Windows.Input;
using Coepd.Mobile.Models;
using Coepd.Mobile.Services;

namespace Coepd.Mobile.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private readonly ApiSession _session;
    private readonly AuthService _authService;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _role = "staff";
    private bool _isPasswordHidden = true;

    public LoginViewModel(ApiSession session, AuthService authService)
    {
        _session = session;
        _authService = authService;
        LoginCommand = new Command(async () => await LoginAsync(), () => !IsLoading);
        TogglePasswordCommand = new Command(() => IsPasswordHidden = !IsPasswordHidden);
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

    public string Role
    {
        get => _role;
        set => SetProperty(ref _role, value);
    }

    public bool IsPasswordHidden
    {
        get => _isPasswordHidden;
        set => SetProperty(ref _isPasswordHidden, value);
    }

    public string ServerDisplay => _session.BaseUrl.Replace("https://", string.Empty).TrimEnd('/');

    public ICommand LoginCommand { get; }
    public ICommand TogglePasswordCommand { get; }

    public void ConfigureDefaultsForRole(string role)
    {
        if (string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
        {
            Email = "admin";
            Password = "admin";
            return;
        }

        if (string.IsNullOrWhiteSpace(Email) || string.Equals(Email, "admin", StringComparison.OrdinalIgnoreCase))
        {
            Email = string.Empty;
        }

        if (string.IsNullOrWhiteSpace(Password) || string.Equals(Password, "admin", StringComparison.Ordinal))
        {
            Password = string.Empty;
        }
    }

    public async Task<(bool Success, string Role, string Error)> LoginAsync(CancellationToken cancellationToken = default)
    {
        if (IsLoading)
        {
            return (false, string.Empty, "Please wait.");
        }

        ErrorMessage = string.Empty;

        var isAdminShortcut = string.Equals(Role, "admin", StringComparison.OrdinalIgnoreCase) &&
                              string.Equals(Email, "admin", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(Email) || (!isAdminShortcut && !Email.Contains('@')))
        {
            ErrorMessage = "Enter a valid work email address.";
            return (false, string.Empty, ErrorMessage);
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Enter your password to continue.";
            return (false, string.Empty, ErrorMessage);
        }

        IsLoading = true;
        ((Command)LoginCommand).ChangeCanExecute();

        try
        {
            var response = await _authService.LoginAsync(new User
            {
                Email = Email,
                Password = Password,
                Role = Role
            }, cancellationToken);

            if (!response.Success)
            {
                ErrorMessage = string.IsNullOrWhiteSpace(response.Error)
                    ? "Unable to access the workspace right now."
                    : response.Error;
                return (false, string.Empty, ErrorMessage);
            }

            return (true, string.IsNullOrWhiteSpace(response.Role) ? Role : response.Role, string.Empty);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return (false, string.Empty, ErrorMessage);
        }
        finally
        {
            IsLoading = false;
            ((Command)LoginCommand).ChangeCanExecute();
        }
    }
}
