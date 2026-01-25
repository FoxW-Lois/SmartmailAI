using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.Resources;

namespace SmartmailAI.ViewModels.Pages;

public partial class Login_ViewModel(IAuthService authService) : ObservableRecipient
{
	private readonly IAuthService _authService = authService;

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

		return (true, false, null);
	}
}
