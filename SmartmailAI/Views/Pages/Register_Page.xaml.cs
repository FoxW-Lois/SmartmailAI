using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SmartmailAI.Views.Pages;

public sealed partial class Register_Page : Page
{
	public Register_ViewModel ViewModel { get; }

	public Register_Page()
	{
		ViewModel = Ioc.Default.GetRequiredService<Register_ViewModel>();
		DataContext = ViewModel;
		InitializeComponent();
	}

	private async void OnRegisterClicked(object sender, RoutedEventArgs e)
	{
		bool success = await ViewModel.RegisterAsync(LoginBox.Text, PhoneNumberBox.Text, PasswordBox.Password, ConfirmPasswordBox.Password);

		if (success)
			Frame.Navigate(typeof(Login_Page));
	}

	private void OnBackToLoginClicked(object sender, RoutedEventArgs e)
	{
		Frame.Navigate(typeof(Login_Page));
	}
}
