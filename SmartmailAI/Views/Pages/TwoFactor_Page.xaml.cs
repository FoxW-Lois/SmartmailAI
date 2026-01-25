using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace SmartmailAI.Views.Pages;

public sealed partial class TwoFactor_Page : Page
{
	public TwoFactor_ViewModel ViewModel { get; }

	public TwoFactor_Page()
	{
		ViewModel = Ioc.Default.GetRequiredService<TwoFactor_ViewModel>();
		DataContext = ViewModel;
		InitializeComponent();
	}

	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);

		if (e.Parameter is string login)
		{
			ViewModel.Initialize(login);
		}
	}
}
