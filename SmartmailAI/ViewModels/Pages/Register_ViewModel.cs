using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Windows.ApplicationModel.Resources;

namespace SmartmailAI.ViewModels.Pages;

public partial class Register_ViewModel(IAuthService authService) : ObservableRecipient
{
	private readonly IAuthService _authService = authService;
	private readonly ResourceLoader resourceLoader = new();

	#region ObservableProperties & View Properties

	[ObservableProperty]
	public partial string Login { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string PhoneNumber { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string? ErrorMessage { get; set; }

	public bool HasError => string.IsNullOrWhiteSpace(ErrorMessage);

	#endregion ObservableProperties & View Properties

	public async Task<bool> RegisterAsync(string login, string phoneNumber, string password, string confirmPassword)
	{
		ErrorMessage = null;

		if (string.IsNullOrWhiteSpace(login))
		{
			ErrorMessage = resourceLoader.GetString("Error_LoginRequired");
			return false;
		}

		if (string.IsNullOrWhiteSpace(phoneNumber))
		{
			ErrorMessage = resourceLoader.GetString("Error_PhoneNumberRequired");
			return false;
		}

		if (!Regex.IsMatch(phoneNumber, @"^\d{10}$"))
		{
			ErrorMessage = resourceLoader.GetString("Error_PhoneNumberInvalid");
			return false;
		}

		// Le mot de passe ne contient pas au moins : une majuscule, une minuscule, un chiffre, un caractère spécial
		if (password.Length < 12 || !Regex.IsMatch(password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).+$"))
		{
			ErrorMessage = resourceLoader.GetString("Error_PasswordInvalid");
			return false;
		}

		if (password != confirmPassword)
		{
			ErrorMessage = resourceLoader.GetString("Error_ConfirmPasswordInvalid");
			return false;
		}

		var (Success, Error) = await _authService.RegisterAsync(login, phoneNumber, password);

		if (!Success)
		{
			ErrorMessage = Error;
			return false;
		}

		ErrorMessage = string.Empty;

		return true;
	}
}
