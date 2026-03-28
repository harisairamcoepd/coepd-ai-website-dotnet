using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Coepd.Mobile.Models;
using Coepd.Mobile.Services;

namespace Coepd.Mobile;

public partial class MainPage : ContentPage, INotifyPropertyChanged
{
	private readonly LeadApiClient _apiClient;
	private string _baseUrl = string.Empty;
	private string _email = string.Empty;
	private string _password = string.Empty;
	private string _searchText = string.Empty;
	private string _statusMessage = "Enter your website URL and staff/admin credentials.";
	private string _sessionRole = "Not logged in";
	private bool _isBusy;

	public new event PropertyChangedEventHandler? PropertyChanged;

	public ObservableCollection<LeadItem> Leads { get; } = new();

	public MainPage(LeadApiClient apiClient)
	{
		InitializeComponent();
		_apiClient = apiClient;
		_baseUrl = _apiClient.BaseUrl;
		BindingContext = this;
	}

	public string BaseUrl
	{
		get => _baseUrl;
		set => SetProperty(ref _baseUrl, value);
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

	public string SearchText
	{
		get => _searchText;
		set => SetProperty(ref _searchText, value);
	}

	public string StatusMessage
	{
		get => _statusMessage;
		set => SetProperty(ref _statusMessage, value);
	}

	public string SessionRole
	{
		get => _sessionRole;
		set => SetProperty(ref _sessionRole, value);
	}

	public string TotalLeads => Leads.Count.ToString();

	private async void OnLoginClicked(object? sender, EventArgs e)
	{
		if (_isBusy) return;
		if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
		{
			StatusMessage = "Enter both email and password.";
			return;
		}

		await RunBusyAsync(async () =>
		{
			_apiClient.BaseUrl = BaseUrl;
			var result = await _apiClient.LoginAsync(Email, Password);
			if (!result.Success)
			{
				StatusMessage = string.IsNullOrWhiteSpace(result.Error) ? "Login failed." : result.Error;
				return;
			}

			SessionRole = string.IsNullOrWhiteSpace(result.Role) ? "Logged in" : result.Role.ToUpperInvariant();
			StatusMessage = "Login successful. Loading leads...";
			await LoadLeadsAsync();
		});
	}

	private async void OnRefreshClicked(object? sender, EventArgs e)
	{
		if (_isBusy) return;
		await RunBusyAsync(LoadLeadsAsync);
	}

	private void OnLogoutClicked(object? sender, EventArgs e)
	{
		Leads.Clear();
		OnPropertyChanged(nameof(TotalLeads));
		SessionRole = "Not logged in";
		StatusMessage = "Session cleared on the app. Login again to reload leads.";
	}

	private async void OnSearchClicked(object? sender, EventArgs e)
	{
		if (_isBusy) return;
		await RunBusyAsync(LoadLeadsAsync);
	}

	private async void OnSearchCompleted(object? sender, EventArgs e)
	{
		if (_isBusy) return;
		await RunBusyAsync(LoadLeadsAsync);
	}

	private async void OnLeadSelected(object? sender, SelectionChangedEventArgs e)
	{
		if (e.CurrentSelection.FirstOrDefault() is not LeadItem lead) return;

		await DisplayAlertAsync(
			lead.Name,
			$"Phone: {lead.Phone}\nEmail: {lead.Email}\nLocation: {lead.Location}\nSource: {lead.SourceLabel}\nCreated: {lead.DateTimeDisplay}",
			"Close");

		((CollectionView)sender!).SelectedItem = null;
	}

	private async Task LoadLeadsAsync()
	{
		_apiClient.BaseUrl = BaseUrl;
		var leads = await _apiClient.GetLeadsAsync(SearchText);

		Leads.Clear();
		foreach (var lead in leads)
		{
			Leads.Add(lead);
		}

		OnPropertyChanged(nameof(TotalLeads));
		StatusMessage = Leads.Count == 0 ? "No leads found for the current search." : $"Loaded {Leads.Count} leads.";
	}

	private async Task RunBusyAsync(Func<Task> action)
	{
		try
		{
			_isBusy = true;
			await action();
		}
		catch (Exception ex)
		{
			StatusMessage = ex.Message;
		}
		finally
		{
			_isBusy = false;
		}
	}

	private void SetProperty<T>(ref T backingField, T value, [CallerMemberName] string? propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(backingField, value)) return;
		backingField = value;
		OnPropertyChanged(propertyName);
	}

	private new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
