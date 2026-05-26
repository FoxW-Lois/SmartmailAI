using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Navigation;
using SmartmailAI.Core.Contracts.Repository;

namespace SmartmailAI.ViewModels.Pages;

public partial class NavShell_ViewModel : ObservableRecipient
{
	[ObservableProperty]
	public partial bool IsBackEnabled { get; set; }

	[ObservableProperty]
	public partial object? Selected { get; set; }

	[ObservableProperty]
	private ObservableCollection<AccountMailBase> accountsMail = [];

	#region Interfaces declaration

	public INavigationService NavigationService { get; }
	public INavigationViewService NavigationViewService { get; }
	public IAuthService _authService { get; }
	public IAddressesRepository _addressesRepository { get; }
	public IAddressesService _addressesService { get; }
	public IEmailsSyncService _emailsSyncService { get; }
	public ILocalSessionService _localSessionService { get; }
	public Login_ViewModel _login_ViewModel { get; }

	#endregion Interfaces declaration

	public NavShell_ViewModel(INavigationService navigationService, INavigationViewService shellService, IAuthService authService,
		IAddressesRepository addressesRepository, IAddressesService addressesService, IEmailsSyncService emailsSyncService,
		ILocalSessionService localSessionService, Login_ViewModel login_ViewModel)
	{
		NavigationService = navigationService;
		NavigationViewService = shellService;
		NavigationService.Navigated += OnNavigated;
		_authService = authService;
		_addressesRepository = addressesRepository;
		_addressesService = addressesService;
		_emailsSyncService = emailsSyncService;
		_localSessionService = localSessionService;
		_login_ViewModel = login_ViewModel;

		// Tente de restaurer la session locale
		_authService.IsAuthenticated = _localSessionService.ValidateSession();

		IsLogged = _authService.IsAuthenticated;
		HasLinkedAddresses = _addressesService.HasAny;

		_authService.AuthenticationStateChanged += (_, isLogged) =>
		{
			// Debug en console du changement de IsAuthenticated
			//Console.WriteLine($"============ Auth changed: {isLogged}");
			IsLogged = isLogged;
			UpdateVisibility();
		};

		// Charge/recharge les adresses emails connectées dans le service au lancement de l'application
		_addressesService.RefreshAddressesListAsync();

		// Si la base de données contient déjà des adresses emails enregistrées/connectées, on les charge dans le NavShell et en plus
		// on lance la synchronisation des emails pour ces comptes
		if (_addressesService.HasAny)
		{
			var listAccountsLinked = _addressesService.GetListAccountsLinkedAsync().GetAwaiter().GetResult();

			foreach (var account in listAccountsLinked)
			{
				var _ = _login_ViewModel.LoadMessagesAsync(account);
			}

			OnAddressesListChanged(true);
		}

		_addressesService.AddressesListChanged += async (_, hasAny) =>
		{
			OnAddressesListChanged(hasAny);
		};

		UpdateVisibility();
	}

	private void OnNavigated(object sender, NavigationEventArgs e)
	{
		// Update the back button status
		IsBackEnabled = NavigationService.CanGoBack;

		// Update the selected NavigationViewItem based on the page type
		var selectedItem = NavigationViewService.GetItem(e.SourcePageType);
		if (selectedItem != null)
		{
			Selected = selectedItem;
		}
	}

	#region Changement d'état concernant l'authentification de l'utilisateur

	public bool _isLogged = false;

	public bool IsLogged
	{
		get => _isLogged;
		set
		{
			if (SetProperty(ref _isLogged, value))
			{
				OnPropertyChanged(nameof(IsNotLogged));
			}
		}
	}

	public bool IsNotLogged => !IsLogged;

	#endregion Changement d'état concernant l'authentification de l'utilisateur

	#region Changement d'état concernant la présence d'adresses email connectées

	private bool _hasLinkedAddresses;

	public bool HasLinkedAddresses
	{
		get => _hasLinkedAddresses;
		set => SetProperty(ref _hasLinkedAddresses, value);
	}

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

	private void OnAddressesListChanged(bool hasAny)
	{
		HasLinkedAddresses = hasAny;
		_ = LoadAccountsAsync();
		UpdateVisibility();

		if (_addressesService.HasAny == true)
		{
			_emailsSyncService.Stop();
			_emailsSyncService.StartAsync();
		}
		else
			_emailsSyncService.Stop();
	}

	#endregion Changement d'état concernant la présence d'adresses email connectées

	public void Logout()
	{
		_emailsSyncService.Stop();
		_authService.Logout();
		_localSessionService.KillSession();
	}
}
