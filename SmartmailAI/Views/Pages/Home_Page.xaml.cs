using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SmartmailAI.Views.Pages;

public sealed partial class Home_Page : Page
{
	public Home_ViewModel ViewModel { get; }

	public Home_Page()
	{
		ViewModel = Ioc.Default.GetRequiredService<Home_ViewModel>();
		DataContext = ViewModel;
		InitializeComponent();

		Loaded += Home_Page_Loaded;
	}

	// Se déclenche quand l'utilisateur ouvre la page Home et tant qu'il n'a pas validé le formulaire des questions UX
	private async void Home_Page_Loaded(object sender, RoutedEventArgs e)
	{
		await ViewModel.InitializeAsync();
	}
}
