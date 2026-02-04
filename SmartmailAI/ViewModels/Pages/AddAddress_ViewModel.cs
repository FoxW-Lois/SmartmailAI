using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.Resources;

namespace SmartmailAI.ViewModels.Pages;

public partial class AddAddress_ViewModel : ObservableRecipient
{
	[ObservableProperty]
	public partial bool IsOtherChoice { get; set; }

	private readonly IAddressesService _addressesService;
	private string _email = string.Empty;
	private string _errorMessage = string.Empty;
	private readonly ResourceLoader resourceLoader = new();

	public AddAddress_ViewModel(IAddressesService emailService)
	{
		_addressesService = emailService;
	}

	public string Email
	{
		get => _email;
		set => SetProperty(ref _email, value);
	}

	public string ErrorMessage
	{
		get => _errorMessage;
		set
		{
			SetProperty(ref _errorMessage, value);
			OnPropertyChanged(nameof(ErrorVisibility));
		}
	}

	public Visibility ErrorVisibility => string.IsNullOrWhiteSpace(ErrorMessage) ? Visibility.Collapsed : Visibility.Visible;

	public async Task<bool> AddOtherAddressAsync(string password)
	{
		ErrorMessage = string.Empty;

		bool success = await _addressesService.AddOtherAddressAsync();

		if (!success)
		{
			ErrorMessage = resourceLoader.GetString("Error_EmailOrPasswordInvalid");
			return false;
		}

		return true;
	}

	public async Task<bool> AddOAuth2Async()
	{
		ErrorMessage = string.Empty;

		(_, bool success) = await _addressesService.AddGmailAccountAsync();

		if (!success)
		{
			ErrorMessage = resourceLoader.GetString("Error_RecoveryEmailOAuth2Invalid");
			return false;
		}

		return true;
	}

	public async Task<bool> OnAddOutlookAsync()
	{
		ErrorMessage = string.Empty;

		bool success = await _addressesService.AddOutlookAsync();

		if (!success)
		{
			ErrorMessage = resourceLoader.GetString("Error_RecoveryEmailOutlookInvalid");
			return false;
		}

		return true;
	}
}
