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

	#region Champs pour la connexion SMTP/IMAP

	// Déclaration avec valeur par défaut :
	private string _email = string.Empty;
	private string _userName = string.Empty;
	private string _password = string.Empty;
	private string _imapHost = string.Empty;
	private int _imapPort = 993;
	private string _imapUseSsl = "true";
	private string _smtpHost = string.Empty;
	private int _smtpPort = 465; // 587 ou 465
	private string _smtpUseSsl = "true";

	public string Email
	{
		get => _email;
		set => SetProperty(ref _email, value);
	}

	public string UserName
	{
		get => _userName;
		set => SetProperty(ref _userName, value);
	}

	public string Password
	{
		get => _password;
		set => SetProperty(ref _password, value);
	}

	public string ImapHost
	{
		get => _imapHost;
		set => SetProperty(ref _imapHost, value);
	}

	public int ImapPort
	{
		get => _imapPort;
		set => SetProperty(ref _imapPort, value);
	}

	public string ImapUseSsl
	{
		get => _imapUseSsl;
		set => SetProperty(ref _imapUseSsl, value);
	}

	public string SmtpHost
	{
		get => _smtpHost;
		set => SetProperty(ref _smtpHost, value);
	}

	public int SmtpPort
	{
		get => _smtpPort;
		set => SetProperty(ref _smtpPort, value);
	}

	public string SmtpUseSsl
	{
		get => _smtpUseSsl;
		set => SetProperty(ref _smtpUseSsl, value);
	}

	#endregion Champs pour la connexion SMTP/IMAP

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
