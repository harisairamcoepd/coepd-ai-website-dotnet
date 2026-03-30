using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Coepd.Mobile.ViewModels;

public abstract class BaseViewModel : INotifyPropertyChanged
{
    private bool _isLoading;
    private string _errorMessage = string.Empty;
    private string _title = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
            {
                OnPropertyChanged(nameof(IsBusy));
            }
        }
    }

    public bool IsBusy
    {
        get => IsLoading;
        set => IsLoading = value;
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public string StatusMessage
    {
        get => ErrorMessage;
        set => ErrorMessage = value;
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
