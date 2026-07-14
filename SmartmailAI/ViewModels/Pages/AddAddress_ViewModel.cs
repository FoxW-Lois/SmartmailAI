using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.Resources;

namespace SmartmailAI.ViewModels.Pages;

public partial class AddAddress_ViewModel(IAddressesService addressesService, IDialogService dialogService, IEmailLoaderService emailLoaderService)
	: ObservableRecipient
{
	[ObservableProperty]
	public partial bool IsOtherChoice { get; set; }

	private readonly IAddressesService _addressesService = addressesService;
	private readonly IDialogService _dialogService = dialogService;
	private readonly IEmailLoaderService _emailLoaderService = emailLoaderService;
	private string _errorMessage = string.Empty;
	private readonly ResourceLoader resourceLoader = new();

	#region ObservableProperties : champs pour la connexion SMTP/IMAP

	[ObservableProperty]
	public partial string Email { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string UserName { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string Password { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string ImapHost { get; set; } = string.Empty;

	[ObservableProperty]
	public partial int ImapPort { get; set; } = 993;

	[ObservableProperty]
	public partial string ImapUseSsl { get; set; } = "true";

	[ObservableProperty]
	public partial string SmtpHost { get; set; } = string.Empty;

	[ObservableProperty]
	public partial int SmtpPort { get; set; } = 465; // 587 ou 465

	[ObservableProperty]
	public partial string SmtpUseSsl { get; set; } = "true";

	#endregion ObservableProperties : champs pour la connexion SMTP/IMAP

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

	#region Add methods for diffrent Addresses type/domain/source

	public async Task<bool> AddOAuth2Async()
	{
		while (!await InternetCheckService.HasInternetConnectionAsync())
		{
			await _dialogService.ShowOneButtonDialogAsync(resourceLoader.GetString("Error_Title"),
				resourceLoader.GetString("Error_HasNoInternet"));
		}

		ErrorMessage = string.Empty;

		(bool success, AccountGmail? accountGmail, string? specificError) = await _addressesService.AddGmailAccountAsync();

		if (!success && specificError == "Email_AlreadyExist")
		{
			ErrorMessage = resourceLoader.GetString("Error_Email_AlreadyExist");
			return false;
		}
		else if (!success)
		{
			ErrorMessage = resourceLoader.GetString("Error_RecoveryMailInvalid");
			return false;
		}

		if (accountGmail is null)
		{
			ErrorMessage = resourceLoader.GetString("Error_RecoveryMailInvalid");
			return false;
		}

		await _addressesService.RefreshAddressesListAsync();
		await _emailLoaderService.LoadMessagesAsync(true, accountGmail);
		return true;
	}

	// TODO: ajouter méthode de connexion pour Outlook (via Microsoft Graph API ?)
	public async Task<bool> OnAddOutlookAsync()
	{
		while (!await InternetCheckService.HasInternetConnectionAsync())
		{
			await _dialogService.ShowOneButtonDialogAsync(resourceLoader.GetString("Error_Title"),
				resourceLoader.GetString("Error_HasNoInternet"));
		}

		ErrorMessage = string.Empty;

		bool success = await _addressesService.AddOutlookAsync();

		if (!success)
		{
			ErrorMessage = resourceLoader.GetString("Error_RecoveryMailInvalid");
			return false;
		}

		await _addressesService.RefreshAddressesListAsync();
		return true;
	}

	public async Task<bool> AddOtherAddressAsync(string userName, string password, string imapHost, int imapPort, bool imapUseSsl,
		string smtpHost, int smtpPort, bool smtpUseSsl)
	{
		while (!await InternetCheckService.HasInternetConnectionAsync())
		{
			await _dialogService.ShowOneButtonDialogAsync(resourceLoader.GetString("Error_Title"),
				resourceLoader.GetString("Error_HasNoInternet"));
		}

		ErrorMessage = string.Empty;

		AddOtherAddressRequest request = new()
		{
			Email = Email,
			UserName = userName,
			Password = password,
			ImapHost = imapHost,
			ImapPort = imapPort,
			ImapUseSsl = imapUseSsl,
			SmtpHost = smtpHost,
			SmtpPort = smtpPort,
			SmtpUseSsl = smtpUseSsl
		};

		(bool success, AccountOther? accountOther, string? specificError) = await _addressesService.AddOtherAddressAsync(request);

		if (!success && specificError == "Email_AlreadyExist")
		{
			ErrorMessage = resourceLoader.GetString("Error_Email_AlreadyExist");
			return false;
		}
		else if (!success)
		{
			ErrorMessage = resourceLoader.GetString("Error_RecoveryMailInvalid");
			return false;
		}

		if (accountOther is null)
		{
			ErrorMessage = resourceLoader.GetString("Error_RecoveryMailInvalid");
			return false;
		}

		await _addressesService.RefreshAddressesListAsync();
		await _emailLoaderService.LoadMessagesAsync(true, accountOther);
		return true;
	}

	#endregion Add methods for diffrent Addresses type/domain/source
}
