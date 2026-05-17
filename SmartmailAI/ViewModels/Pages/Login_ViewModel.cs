using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.Resources;
using SmartmailAI.Core.Contracts.Services.Addresses;
using SmartmailAI.Core.Contracts.Repository;

namespace SmartmailAI.ViewModels.Pages;

public partial class Login_ViewModel(IAuthService authService, IMailReaderService mailReaderService, IEmailRepository emailRepository,
	IAddressesService addressesService) : ObservableRecipient
{
	private readonly IAuthService _authService = authService;
	private readonly IMailReaderService _mailReaderService = mailReaderService;
	private readonly IEmailRepository _emailRepository = emailRepository;
	private readonly IAddressesService _addressesService = addressesService;

	private string _login = string.Empty;
	private string _errorMessage = string.Empty;
	private readonly ResourceLoader resourceLoader = new();

	public string Login
	{
		get => _login;
		set => SetProperty(ref _login, value);
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

	public async Task<(bool success, bool twoFactorValidation, string?)> LoginAsync(string password)
	{
		ErrorMessage = string.Empty;

		(bool success, string? specificError) = await _authService.LoginAsync(Login, password);

		if (!success && specificError == "Need_TwoFactor")
			return (false, true, Login);

		if (!success && specificError != null)
		{
			ErrorMessage = resourceLoader.GetString(specificError);
			return (false, false, null);
		}

		if (!success)
		{
			ErrorMessage = resourceLoader.GetString("Error_LoginOrPasswordInvalid");
			return (false, false, null);
		}

		var listAccountsLinked = await _addressesService.GetListAccountsLinkedAsync();

		foreach (var account in listAccountsLinked)
		{
			if (account is AccountGmail accountGmail)
				await LoadMessagesAsync(accountGmail: accountGmail);
			else if (account is AccountOther accountOther)
				await LoadMessagesAsync(accountOther: accountOther);
		}

		return (true, false, null);
	}

	public ObservableCollection<EmailFromAddress> Messages { get; } = [];

	public async Task LoadMessagesAsync(AccountGmail? accountGmail = null, AccountOther? accountOther = null)
	{
		Messages.Clear();

		var mails = await _mailReaderService.GetLastMessagesFromAccountAsync(false, accountGmail: accountGmail,
			accountOther: accountOther);

		foreach (var email in mails)
			await _emailRepository.AddEmailAsync(email);

		await _authService.UpdateLastConnection();
	}
}
