using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using SmartmailAI.Core.Contracts.Repository;
using SmartmailAI.Core.Models.Messengers;

namespace SmartmailAI.ViewModels.Pages;

public partial class Settings_ViewModel : ObservableRecipient, INavigationAware
{
	#region View Properties

	public ObservableCollection<AppLanguageItem> AppLanguages = AppLanguageHelper.SupportedLanguages;

	public Visibility NonlogonTaskCardVisibility = RuntimeHelper.IsMSIX ? Visibility.Visible : Visibility.Collapsed;
	public Visibility LogonTaskExpanderVisibility = RuntimeHelper.IsMSIX ? Visibility.Collapsed : Visibility.Visible;

	public Visibility NoAccountLoggedInVisibility = Visibility.Visible;
	public Visibility AccountLoggedInVisibility = Visibility.Collapsed;

	#endregion View Properties

	#region ObservableProperty

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

	[ObservableProperty]
	public partial int NbOpenAppByWeek { get; set; } = 0;

	[ObservableProperty]
	public partial ObservableCollection<string> AverageDailyTraficOptions { get; set; } = ["1 à 30 mails par jour",
		"30 à 60 mails par jour", "60 à 90 mails par jour", "+ de 90 mails par jour"];

	[ObservableProperty]
	public partial string SelectedAverageDailyTrafic { get; set; } = string.Empty;

	[ObservableProperty]
	public partial bool RetrievedAllEmails { get; set; }

	[ObservableProperty]
	public partial DateTimeOffset? DatePicked { get; set; }

	[ObservableProperty]
	public partial bool IsItemsEnabled { get; set; } = false;

	#endregion ObservableProperty

	private readonly IAppSettingsService _appSettingsService;
	private readonly IBackdropSelectorService _backdropSelectorService;
	private readonly IThemeSelectorService _themeSelectorService;
	private readonly IAuthService _authService;
	private readonly INavigationService _navigationService;
	private readonly IAccountRepository _accountRepository;
	private Account? account;

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

		// Quand reçoit une demande, mets les Iteams en Enabled
		WeakReferenceMessenger.Default.Register<RequestUpdateUXQuestionsMessage>(this, async (r, m) =>
		{
			IsItemsEnabled = true;

			account = await _accountRepository.GetAccountByLoginAsync(_authService.CurrentAccountLogin);

			UpdateFieldsFromAccount();
		});
	}

	private async void InitializeSettings()
	{
		ThemeIndex = (int)_themeSelectorService.Theme;
		BackdropTypeIndex = (int)_appSettingsService.BackdropType;

		account = await _accountRepository.GetAccountByLoginAsync(_authService.CurrentAccountLogin);

		if (account is not null && account.IsFirstConnection is false)
			IsItemsEnabled = true;

		UpdateFieldsFromAccount();

		_isInitialized = true;
	}

	private void UpdateFieldsFromAccount()
	{
		if (account is not null)
		{
			NbOpenAppByWeek = account.NbOpenAppByWeek.GetValueOrDefault(0);
			SelectedAverageDailyTrafic = account.AverageDailyTrafic ?? "";
			RetrievedAllEmails = account.RetrievedAllEmails ?? false;
			DatePicked = account.DatePicked?.ToDateTime(TimeOnly.MinValue);
		}
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

	#endregion UI Elements Update

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

	partial void OnThemeIndexChanged(int value)
	{
		if (_isInitialized)
		{
			_themeSelectorService.SetThemeAsync((ElementTheme)value);
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

	async partial void OnNbOpenAppByWeekChanging(int value)
	{
		if (account is null || value < 1 || value > 100)
			return;

		account.NbOpenAppByWeek = value;
		await _accountRepository.UpdateAccountAsync(account);
	}

	async partial void OnSelectedAverageDailyTraficChanged(string value)
	{
		if (account is null || string.IsNullOrWhiteSpace(value))
			return;

		account.AverageDailyTrafic = value;
		await _accountRepository.UpdateAccountAsync(account);
	}

	async partial void OnRetrievedAllEmailsChanged(bool value)
	{
		if (account is null)
			return;

		account.RetrievedAllEmails = value;
		await _accountRepository.UpdateAccountAsync(account);
	}

	async partial void OnDatePickedChanging(DateTimeOffset? value)
	{
		if (account is null || !value.HasValue && account.RetrievedAllEmails is false)
			return;

		account.DatePicked = DateOnly.FromDateTime(DateTime.Parse(value!.Value.ToString("yyyy-MM-dd")));
		await _accountRepository.UpdateAccountAsync(account);
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
