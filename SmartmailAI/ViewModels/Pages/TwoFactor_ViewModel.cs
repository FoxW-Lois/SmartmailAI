using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Windows.ApplicationModel.Resources;

namespace SmartmailAI.ViewModels.Pages;

public partial class TwoFactor_ViewModel : ObservableObject
{
	private readonly IAuthService _authService;
	private readonly INavigationService _navigationService;

	#region ObservableProperties & View Properties

	[ObservableProperty]
	public partial string Code { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string? ErrorMessage { get; set; }

	public bool HasError => string.IsNullOrWhiteSpace(ErrorMessage);

	#endregion ObservableProperties & View Properties

	public string Login { get; private set; } = string.Empty;
	private readonly ResourceLoader resourceLoader = new();
	public ICommand ValidateCommand { get; }

	public TwoFactor_ViewModel(IAuthService authService, INavigationService navigation)
	{
		_authService = authService;
		_navigationService = navigation;

		ValidateCommand = new AsyncRelayCommand(ValidateAsync);
	}

	public void Initialize(string login)
	{
		Login = login;
		OnPropertyChanged(nameof(Login));
	}

	private async Task ValidateAsync()
	{
		if (await _authService.ValidateSecondFactorAsync(Login, Code))
			_navigationService.NavigateTo(typeof(Home_ViewModel).FullName!);
		else
		{
			ErrorMessage = resourceLoader.GetString("Error_SecondFactorInvalid");
		}
	}
}
