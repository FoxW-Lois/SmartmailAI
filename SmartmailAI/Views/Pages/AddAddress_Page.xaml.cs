using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SmartmailAI.Views.Pages;

public sealed partial class AddAddress_Page : Page
{
	public AddAddress_ViewModel ViewModel { get; }

	public AddAddress_Page()
	{
		ViewModel = Ioc.Default.GetRequiredService<AddAddress_ViewModel>();
		DataContext = ViewModel;
		InitializeComponent();
	}

	private async void OnAddClicked(object sender, RoutedEventArgs e)
	{
		bool success = await ViewModel.AddOtherAddressAsync(PasswordBox.Password);

		if (success)
		{
			Frame.Navigate(typeof(DetailsList_Page));
			// Nettoie l'historique de navigation
			Frame.BackStack.Clear();
		}
	}

	private async void OnAddOAuth2Clicked(object sender, RoutedEventArgs e)
	{
		bool success = await ViewModel.AddOAuth2Async();

		if (success)
		{
			Frame.Navigate(typeof(AddressManagement_Page));
			// Nettoie l'historique de navigation
			Frame.BackStack.Clear();
		}
	}

	private async void OnAddOutlookClicked(object sender, RoutedEventArgs e)
	{
		bool success = await ViewModel.OnAddOutlookAsync();

		if (success)
		{
			Frame.Navigate(typeof(AddressManagement_Page));
			// Nettoie l'historique de navigation
			Frame.BackStack.Clear();
		}
	}

	private async void OnOtherChoiceClicked(object sender, RoutedEventArgs e)
	{
		ViewModel.IsOtherChoice = !ViewModel.IsOtherChoice;
	}
}
