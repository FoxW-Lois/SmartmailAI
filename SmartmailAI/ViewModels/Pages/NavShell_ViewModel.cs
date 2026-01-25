using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Navigation;

namespace SmartmailAI.ViewModels.Pages;

public partial class NavShell_ViewModel : ObservableRecipient
{
	[ObservableProperty]
	public partial bool IsBackEnabled { get; set; }

	[ObservableProperty]
	public partial object? Selected { get; set; }

	public INavigationService NavigationService { get; }

	public INavigationViewService NavigationViewService { get; }

	public IAuthService _authService { get; }

	public NavShell_ViewModel(INavigationService navigationService, INavigationViewService shellService, IAuthService authService)
	{
		NavigationService = navigationService;
		NavigationViewService = shellService;
		NavigationService.Navigated += OnNavigated;
		_authService = authService;

		IsLogged = _authService.IsAuthenticated;

		_authService.AuthenticationStateChanged += (_, isLogged) =>
		{
			// Debug en console du changement de IsAuthenticated
			//Console.WriteLine($"============ Auth changed: {isLogged}");
			IsLogged = isLogged;
		};
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

	public void Logout()
	{
		_authService.Logout();
	}
}
