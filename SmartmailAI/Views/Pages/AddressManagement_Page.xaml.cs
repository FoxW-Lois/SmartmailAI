using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SmartmailAI.Views.Pages;

public sealed partial class AddressManagement_Page : Page
{
	public AddressManagement_ViewModel ViewModel { get; }

	public AddressManagement_Page()
	{
		ViewModel = Ioc.Default.GetRequiredService<AddressManagement_ViewModel>();
		DataContext = ViewModel;
		InitializeComponent();
		Loaded += async (_, _) => await ViewModel.LoadAddressesAsync();
	}

	private async void OnAddressDeleteClicked(object sender, RoutedEventArgs e)
	{
		if (sender is not Button button) return;
		if (button.CommandParameter is not AccountGmail accountGmail || button.CommandParameter is not AccountOther accountOther) return;

		await ViewModel.DeleteAddressAsync(accountGmail: accountGmail, accountOther: accountOther);
	}
}
