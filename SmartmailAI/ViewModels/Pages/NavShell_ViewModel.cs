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


	public NavShell_ViewModel(INavigationService navigationService, INavigationViewService shellService/*, IAuthService authService*/)
	{
		NavigationService = navigationService;
		NavigationViewService = shellService;
		NavigationService.Navigated += OnNavigated;
		//_authService = authService;
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
}
