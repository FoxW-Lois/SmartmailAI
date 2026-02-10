using System.Collections.ObjectModel;
using System.Net.Mail;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.Resources;
using SmartmailAI.Core.Contracts.Repository;
using SmartmailAI.Core.Contracts.Services.Addresses;

namespace SmartmailAI.ViewModels.Pages;

public partial class AddAddress_ViewModel(IAddressesService addressesService, IMailReaderService railReaderService,
	IEmailRepository emailRepository, IAuthService authService) : ObservableRecipient
{
	[ObservableProperty]
	public partial bool IsOtherChoice { get; set; }

	private readonly IAddressesService _addressesService = addressesService;
	private readonly IMailReaderService _mailReaderService = railReaderService;
	private readonly IEmailRepository _emailRepository = emailRepository;
	private readonly IAuthService _authService = authService;
	private string _email = string.Empty;
	private string _errorMessage = string.Empty;
	private readonly ResourceLoader resourceLoader = new();

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

	public ObservableCollection<EmailGmail> Messages { get; } = [];

	public async Task LoadMessagesAsync(AccountGmail accountGmail)
	{
		Messages.Clear();

		var mails = await _mailReaderService.GetLastMessagesFromAccountAsync(accountGmail, true);

		foreach (var email in mails)
			await _emailRepository.AddAsync(email);

		await _authService.UpdateLastConnection();
	}

	#region Add methods for diffrent Addresses type/domain/source

	public async Task<bool> AddOAuth2Async()
	{
		ErrorMessage = string.Empty;

		(bool success, AccountGmail? accountGmail, string? specificError) = await _addressesService.AddGmailAccountAsync();

		if (!success && specificError == "Email_AlreadyExist")
		{
			ErrorMessage = resourceLoader.GetString("Error_Email_AlreadyExist");
			return false;
		}
		else if (!success)
		{
			ErrorMessage = resourceLoader.GetString("Error_RecoveryEmailOAuth2Invalid");
			return false;
		}

		if (accountGmail == null)
		{
			ErrorMessage = resourceLoader.GetString("Error_RecoveryEmailOAuth2Invalid");
			return false;
		}

		await _addressesService.RefreshAddressesListAsync();
		await LoadMessagesAsync(accountGmail);
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

		await _addressesService.RefreshAddressesListAsync();
		return true;
	}

	public async Task<bool> AddOtherAddressAsync(string password)
	{
		ErrorMessage = string.Empty;

		bool success = await _addressesService.AddOtherAddressAsync();

		if (!success)
		{
			ErrorMessage = resourceLoader.GetString("Error_EmailOrPasswordInvalid");
			return false;
		}

		await _addressesService.RefreshAddressesListAsync();
		return true;
	}

	#endregion Add methods for diffrent Addresses type/domain/source
}
