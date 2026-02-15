using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SmartmailAI.Views.Pages;

public sealed partial class NavShell_Page : Page
{
	public NavShell_ViewModel ViewModel { get; }

	public Frame ShellFrame => NavigationFrame;

	private readonly INavigationService _navigationService;

	public NavShell_Page(INavigationService navigationService)
	{
		ViewModel = Ioc.Default.GetRequiredService<NavShell_ViewModel>();
		DataContext = ViewModel;
		InitializeComponent();

#if TRAY_ICON
		var trayIcon = new TrayMenuControl();
		ContentArea.Children.Add(trayIcon);

		App.TrayIcon = trayIcon;
#endif

		ViewModel.NavigationService.Frame = ShellFrame;
		ViewModel.NavigationViewService.Initialize(NavigationViewControl);

		// A custom title bar is required for full window theme and Mica support.
		// https://docs.microsoft.com/windows/apps/develop/title-bar?tabs=winui3#full-customization
		App.MainWindow.ExtendsContentIntoTitleBar = true;
		App.MainWindow.SetTitleBar(AppTitleBar);
		App.MainWindow.TitleBar = AppTitleBar;

		AppTitleBarText.Text = ConstantHelper.AppDisplayName;

		App.MainWindow.Activated += MainWindow_Activated;

		_navigationService = navigationService;
	}

	private void Page_Loaded(object sender, RoutedEventArgs e)
	{
		TitleBarHelper.UpdateTitleBar(App.MainWindow, RequestedTheme);
	}

	private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
	{
		App.MainWindow.Activated -= MainWindow_Activated;
		App.MainWindow.TitleBarText = AppTitleBarText;
	}

	private void NavigationViewControl_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
	{
		AppTitleBar.Margin = new Thickness()
		{
			Left = sender.CompactPaneLength * (sender.DisplayMode == NavigationViewDisplayMode.Minimal ? 2 : 1),
			Top = AppTitleBar.Margin.Top,
			Right = AppTitleBar.Margin.Right,
			Bottom = AppTitleBar.Margin.Bottom
		};
	}

	private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
	{
		if (args.InvokedItemContainer?.Tag?.ToString() == "Logout")
		{
			ViewModel.Logout();

			NavigationFrame.Navigate(typeof(Login_Page));

			// Nettoie l'historique de navigation
			NavigationFrame.BackStack.Clear();
		}

		if (args.InvokedItemContainer?.DataContext is AccountGmail account)
		{
			string addressAccount = account.Email;
			_navigationService.NavigateTo(typeof(DetailsList_ViewModel).FullName!, addressAccount);
		}
	}
}
