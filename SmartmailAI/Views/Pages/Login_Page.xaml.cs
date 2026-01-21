using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SmartmailAI.Views.Pages;

public sealed partial class Login_Page : Page
{
	public Login_ViewModel ViewModel { get; }

	public Login_Page()
	{
		ViewModel = Ioc.Default.GetRequiredService<Login_ViewModel>();
		DataContext = ViewModel;
		InitializeComponent();
	}

	private async void OnLoginClicked(object sender, RoutedEventArgs e)
	{
		await ViewModel.LoginAsync(PasswordBox.Password);
		//Frame.Navigate(typeof(Home_Page));

	}

	private void OnRegisterClicked(object sender, RoutedEventArgs e)
	{
		Frame.Navigate(typeof(Register_Page));
	}
}
