using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace SmartmailAI.Views.Pages;

public sealed partial class SettingsTwoFactor_Page : Page
{
	public SettingsTwoFactor_ViewModel ViewModel { get; }

	public SettingsTwoFactor_Page()
	{
		ViewModel = Ioc.Default.GetRequiredService<SettingsTwoFactor_ViewModel>();
		DataContext = ViewModel;
		InitializeComponent();
	}
}
