using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.ApplicationModel.Resources;
using SmartmailAI.Core.Contracts.Repository;
using SmartmailAI.Core.Models.Messengers;

namespace SmartmailAI.ViewModels.Pages;

public partial class NavShell_ViewModel : ObservableRecipient
{
	#region ObservableProperties

	[ObservableProperty]
	public partial bool IsBackEnabled { get; set; }

	[ObservableProperty]
	public partial object? Selected { get; set; }

	[ObservableProperty]
	public partial ObservableCollection<AccountMailBase> AccountsMail { get; set; } = [];

	[ObservableProperty]
	public partial bool IsItemsEnabled { get; set; } = false;

	[ObservableProperty]
	public partial bool HasLinkedAddresses { get; set; } = false;

	[ObservableProperty]
	public partial bool IsLogged { get; set; } = false;

	#endregion ObservableProperties

	#region Interfaces declaration

	public INavigationService NavigationService { get; }
	public INavigationViewService NavigationViewService { get; }
	private readonly IAuthService _authService;
	private readonly IAccountService _accountService;
	private readonly IAddressesRepository _addressesRepository;
	private readonly IAddressesService _addressesService;
	private readonly IEmailsSyncService _emailsSyncService;
	private readonly ILocalSessionService _localSessionService;
	private readonly IDialogService _dialogService;
	private readonly IEmailLoaderService _emailLoaderService;
	private readonly ResourceLoader resourceLoader = new();

	#endregion Interfaces declaration

	public NavShell_ViewModel(INavigationService navigationService, INavigationViewService shellService, IAuthService authService,
		IAddressesRepository addressesRepository, IAccountService accountService, IAddressesService addressesService,
		IEmailsSyncService emailsSyncService, ILocalSessionService localSessionService, IDialogService dialogService,
		IEmailLoaderService emailLoaderService)
	{
		NavigationService = navigationService;
		NavigationViewService = shellService;
		NavigationService.Navigated += OnNavigated;
		_authService = authService;
		_accountService = accountService;
		_addressesRepository = addressesRepository;
		_addressesService = addressesService;
		_emailsSyncService = emailsSyncService;
		_localSessionService = localSessionService;
		_dialogService = dialogService;
		_emailLoaderService = emailLoaderService;

		// Tente de restaurer la session locale
		_authService.IsAuthenticated = _localSessionService.ValidateSession();
		IsLogged = _authService.IsAuthenticated;

		// Quand reçoit une demande, mets les Iteams en Enabled
		WeakReferenceMessenger.Default.Register<RequestUpdateUXQuestionsMessage>(this, async (r, m) =>
		{
			IsItemsEnabled = true;
		});

		_authService.AuthenticationStateChanged += (_, isLogged) =>
		{
			// Debug en console du changement de IsAuthenticated
			//Console.WriteLine($"============ Auth changed: {isLogged}");
			IsLogged = isLogged;
			UpdateVisibility();
		};

		_addressesService.AddressesListChanged += async (_, hasAny) =>
		{
			HasLinkedAddresses = hasAny;
			await LoadAccountsAsync();
			UpdateVisibility();

			if (hasAny)
				await _emailsSyncService.StartAsync();
			else
				_emailsSyncService.Stop();
		};

		UpdateVisibility();
	}

	public async Task InitializeAsync()
	{
		var userAccount = await _accountService.GetAccountByLoginInLocalSessionAsync();

		if (userAccount is null)
			return;

		if (userAccount.IsFirstConnection is false)
			IsItemsEnabled = true;

		while (!await InternetCheckService.HasInternetConnectionAsync())
		{
			await _dialogService.ShowOneButtonDialogAsync(resourceLoader.GetString("Error_Title"),
				resourceLoader.GetString("Error_HasNoInternet"));
		}

		// Charge/recharge les adresses emails connectées dans le service au lancement de l'application
		await _addressesService.RefreshAddressesListAsync();
		HasLinkedAddresses = _addressesService.HasAny;

		// Si la base de données contient déjà des adresses emails enregistrées/connectées, on les charge dans le NavShell et en plus
		// on lance la synchronisation des emails pour ces comptes
		if (_addressesService.HasAny)
		{
			var accounts = await _addressesService.GetListAccountsLinkedAsync();

			foreach (var account in accounts)
			{
				await _emailLoaderService.LoadMessagesAsync(false, account);
			}

			await LoadAccountsAsync();

			await _emailsSyncService.StartAsync();
		}

		UpdateVisibility();
	}

	private void OnNavigated(object sender, NavigationEventArgs e)
	{
		// Update the back button status
		IsBackEnabled = NavigationService.CanGoBack;

		// Update the selected NavigationViewItem based on the page type
		var selectedItem = NavigationViewService.GetItem(e.SourcePageType);
		if (selectedItem is not null)
		{
			Selected = selectedItem;
		}
	}

	#region Changement d'état concernant la présence d'adresses email connectées

	public bool CanShowAddressManagement => IsLogged && HasLinkedAddresses;

	private void UpdateVisibility()
	{
		OnPropertyChanged(nameof(CanShowAddressManagement));
	}

	public async Task LoadAccountsAsync()
	{
		var accounts = await _addressesRepository.GetAllAddressesAsync();

		AccountsMail.Clear();

		foreach (var account in accounts)
			AccountsMail.Add(account);
	}

	#endregion Changement d'état concernant la présence d'adresses email connectées

	public void Logout()
	{
		_emailsSyncService.Stop();
		_authService.Logout();
		_localSessionService.KillSession();
	}
}
