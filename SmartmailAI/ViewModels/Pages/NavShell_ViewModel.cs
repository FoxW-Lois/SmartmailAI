using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Navigation;
using SmartmailAI.Core.IRepository;

namespace SmartmailAI.ViewModels.Pages;

public partial class NavShell_ViewModel : ObservableRecipient
{
	[ObservableProperty]
	public partial bool IsBackEnabled { get; set; }

	[ObservableProperty]
	public partial object? Selected { get; set; }

	[ObservableProperty]
	private ObservableCollection<AccountGmail> accountsGmail = [];

	#region Interfaces declaration

	public INavigationService NavigationService { get; }

	public INavigationViewService NavigationViewService { get; }

	public IAuthService _authService { get; }

	public IAddressesRepository _addressesRepository { get; }

	public IAddressesService _addressesService { get; }

	#endregion Interfaces declaration

	public NavShell_ViewModel(INavigationService navigationService, INavigationViewService shellService, IAuthService authService,
		IAddressesRepository addressesRepository, IAddressesService addressesService)
	{
		NavigationService = navigationService;
		NavigationViewService = shellService;
		NavigationService.Navigated += OnNavigated;
		_authService = authService;
		_addressesRepository = addressesRepository;
		_addressesService = addressesService;

		IsLogged = _authService.IsAuthenticated;
		HasLinkedAddresses = _addressesService.HasAny;

		_authService.AuthenticationStateChanged += (_, isLogged) =>
		{
			// Debug en console du changement de IsAuthenticated
			//Console.WriteLine($"============ Auth changed: {isLogged}");
			IsLogged = isLogged;
			UpdateVisibility();
		};

		_addressesService.AddressesListChanged += (_, hasAny) =>
		{
			HasLinkedAddresses = hasAny;
			UpdateVisibility();
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

	#endregion Changement d'état concernant la présence d'adresses email connectées

	public void Logout()
	{
		_authService.Logout();
	}
}
