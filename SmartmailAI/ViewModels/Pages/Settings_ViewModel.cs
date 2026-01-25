using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using SmartmailAI.Core.IRepository;

namespace SmartmailAI.ViewModels.Pages;

public partial class Settings_ViewModel : ObservableRecipient, INavigationAware
{
	#region View Properties

	public ObservableCollection<AppLanguageItem> AppLanguages = AppLanguageHelper.SupportedLanguages;

	public Visibility NonlogonTaskCardVisibility = RuntimeHelper.IsMSIX ? Visibility.Visible : Visibility.Collapsed;
	public Visibility LogonTaskExpanderVisibility = RuntimeHelper.IsMSIX ? Visibility.Collapsed : Visibility.Visible;

	[ObservableProperty]
	public Visibility noAccountLoggedInVisibility = Visibility.Visible;
	[ObservableProperty]
	public Visibility accountLoggedInVisibility = Visibility.Collapsed;

	[ObservableProperty]
	public partial int LanguageIndex { get; set; }

	[ObservableProperty]
	public partial bool ShowRestartTip { get; set; }

	[ObservableProperty]
	public partial bool RunStartup { get; set; }

	[ObservableProperty]
	public partial bool LogonTask { get; set; }

	[ObservableProperty]
	public partial int ThemeIndex { get; set; }

	[ObservableProperty]
	public partial int BackdropTypeIndex { get; set; }

	[ObservableProperty]
	public partial bool EnableDisableTwoFactor { get; set; }

	[ObservableProperty]
	public partial string AppDisplayName { get; set; } = ConstantHelper.AppDisplayName;

	[ObservableProperty]
	public partial string Version { get; set; } = $"v{InfoHelper.GetVersion()}";

	[ObservableProperty]
	public partial string CopyRight { get; set; } = $"{InfoHelper.GetCopyright()}";

	#endregion View Properties

	private readonly IAppSettingsService _appSettingsService;
	private readonly IBackdropSelectorService _backdropSelectorService;
	private readonly IThemeSelectorService _themeSelectorService;
	private readonly IAuthService _authService;
	private readonly INavigationService _navigationService;
	private readonly IAccountRepository _accountRepository;

	private bool _isInitialized;

	public Settings_ViewModel(IAppSettingsService appSettingsService, IBackdropSelectorService backdropSelectorService,
		IThemeSelectorService themeSelectorService, IAuthService authService, INavigationService navigationService,
		IAccountRepository accountRepository)
	{
		_appSettingsService = appSettingsService;
		_backdropSelectorService = backdropSelectorService;
		_themeSelectorService = themeSelectorService;
		_authService = authService;
		_navigationService = navigationService;
		_accountRepository = accountRepository;

		// Abonnement à l’événement
		_authService.AuthenticationStateChanged += OnAuthenticationStateChanged;
		// Initialisation de l’état
		UpdateVisibilyProperties(_authService.IsAuthenticated);

		InitializeSettings();
	}

	private async void InitializeSettings()
	{
		ThemeIndex = (int)_themeSelectorService.Theme;
		BackdropTypeIndex = (int)_appSettingsService.BackdropType;

		var account = await _accountRepository.GetByLoginAsync(_authService.CurrentAccountLogin);
		if (account != null)
			var stateTwoFactor = account.TwoFactorEnabled;
			EnableDisableTwoFactor = stateTwoFactor;
		}

		_isInitialized = true;
	}

	#region UI Elements Update

	private void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
	{
		UpdateVisibilyProperties(isAuthenticated);
	}

	public void UpdateVisibilyProperties(bool isAuthenticated)
	{
		AccountLoggedInVisibility = isAuthenticated ? Visibility.Visible : Visibility.Collapsed;

		NoAccountLoggedInVisibility = isAuthenticated ? Visibility.Collapsed : Visibility.Visible;
	}

	#endregion

	#region INavigationAware

	public async Task OnNavigatedTo(object parameter)
	{
		LanguageIndex = AppLanguageHelper.SupportedLanguages.IndexOf(AppLanguageHelper.PreferredLanguage);

		var logonTask = await StartupHelper.GetStartupAsync(logon: true);
		var startupEntry = await StartupHelper.GetStartupAsync();
		RunStartup = logonTask || startupEntry;
		LogonTask = logonTask;

		ShowRestartTip = false;
	}

	public void OnNavigatedFrom()
	{
	}

	#endregion INavigationAware

	#region Commands

#pragma warning disable CA1822 // Mark members as static

	[RelayCommand]
	private void RestartApplication()
	{
		App.RestartApplication();
	}

#pragma warning restore CA1822 // Mark members as static

	[RelayCommand]
	private void CancelRestart()
	{
		ShowRestartTip = false;
	}

	#endregion Commands

	#region Property Events

	partial void OnLanguageIndexChanging(int value)
	{
		if (_isInitialized)
		{
			if (RuntimeHelper.IsMSIX)
			{
				// No need to store the preference in packaged app - it is already stored by the app
				AppLanguageHelper.TryChange(value);
			}
			else
			{
				// No need to set PrimaryLanguageOverride in unpackaged app - it will be set by the app in the next launch
				_appSettingsService.SetLanguageAsync(AppLanguageHelper.GetLanguageCode(value));
			}

			ShowRestartTip = true;
		}
	}

	partial void OnRunStartupChanged(bool value)
	{
		if (_isInitialized)
		{
			if (value)
			{
				_ = StartupHelper.SetStartupAsync(true, logon: LogonTask);
			}
			else
			{
				_ = StartupHelper.SetStartupAsync(false, logon: true);
				_ = StartupHelper.SetStartupAsync(false);
			}
		}
	}

	partial void OnLogonTaskChanged(bool value)
	{
		if (_isInitialized)
		{
			if (RunStartup)
			{
				_ = StartupHelper.SetStartupAsync(false, logon: !value);
				_ = StartupHelper.SetStartupAsync(true, logon: value);
			}
		}
	}

	async partial void OnEnableDisableTwoFactorChanged(bool value)
	{
		if (!_isInitialized || !_authService.IsAuthenticated)
			return;

		await Task.Delay(3000); // Attente de 3 secondes

		var targetViewModel = value ? typeof(SettingsTwoFactor_ViewModel) : typeof(Home_ViewModel);

		_navigationService.NavigateTo(targetViewModel.FullName!);

		await _authService.Update_EnableDisable_TwoFactorAsync(_authService.CurrentAccountLogin, value); // Value : true = activer, false = désactiver
	}

	partial void OnThemeIndexChanged(int value)
	{
		if (_isInitialized)
		{
			_themeSelectorService.SetThemeAsync((ElementTheme)value);
		}
	}

	partial void OnBackdropTypeIndexChanged(int value)
	{
		if (_isInitialized)
		{
			_backdropSelectorService.SetBackdropTypeAsync((BackdropType)value);
		}
	}

	#endregion Property Events
}
