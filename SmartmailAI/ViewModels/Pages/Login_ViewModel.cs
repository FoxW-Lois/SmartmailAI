using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Windows.ApplicationModel.Resources;
using SmartmailAI.Core.Models.Messengers;

namespace SmartmailAI.ViewModels.Pages;

public partial class Login_ViewModel(IAuthService authService, IAccountService accountService, IAddressesService addressesService,
	IEmailLoaderService emailLoaderService, ILocalSessionService localSessionService) : ObservableRecipient
{
	private readonly IAuthService _authService = authService;
	private readonly IAccountService _accountService = accountService;
	private readonly IAddressesService _addressesService = addressesService;
	private readonly IEmailLoaderService _emailLoaderService = emailLoaderService;
	private readonly ILocalSessionService _localSessionService = localSessionService;
	private readonly ResourceLoader resourceLoader = new();

	#region ObservableProperties & View Properties

	[ObservableProperty]
	public partial string Login { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string? ErrorMessage { get; set; }

	public bool HasError => string.IsNullOrWhiteSpace(ErrorMessage);

	#endregion ObservableProperties & View Properties

	public async Task<(bool success, bool twoFactorValidation, string?)> LoginAsync(string password)
	{
		ErrorMessage = null;

		(bool success, string? specificError) = await _authService.LoginAsync(Login, password);

		if (!success && specificError == "Need_TwoFactor")
			return (false, true, Login);

		if (!success && specificError is not null)
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
			await _emailLoaderService.LoadMessagesAsync(false, account);
		}

		_localSessionService.CreateSession();

		Login = string.Empty;
		ErrorMessage = null;

		var userAccount = await _accountService.GetAccountByLoginInLocalSessionAsync();

		if (userAccount is not null && userAccount.IsFirstConnection is false)
		{
			// Notifie Home_ViewModel, NavShell_ViewModel et Settings_ViewModel de mettre à jour leur vue respective
			WeakReferenceMessenger.Default.Send(new RequestUpdateUXQuestionsMessage { ChangeDisplay = true });
		}
		else if (userAccount is not null && userAccount.IsFirstConnection is true)
		{
			// Notifie Home_ViewModel, NavShell_ViewModel et Settings_ViewModel de mettre à jour leur vue respective
			WeakReferenceMessenger.Default.Send(new RequestUpdateUXQuestionsMessage { ChangeDisplay = false });
		}

		return (true, false, null);
	}
}
