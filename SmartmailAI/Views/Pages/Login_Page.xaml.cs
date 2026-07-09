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
		(bool success, bool twoFactorValidation, string? login) = await ViewModel.LoginAsync(PasswordBox.Password);

		// Normalement twoFactorValidation est false, mais au cas où on envoie sur la page du 2ème facteur
		// "login" un string nullable mais ici il n'aura jamais null
		if (twoFactorValidation)
			Frame.Navigate(typeof(TwoFactor_Page), login);

		if (success)
		{
			Frame.Navigate(typeof(Home_Page));
			// Nettoie l'historique de navigation
			Frame.BackStack.Clear();

			PasswordBox.Password = string.Empty;
		}
	}

	private void OnRegisterClicked(object sender, RoutedEventArgs e)
	{
		Frame.Navigate(typeof(Register_Page));
	}
}
