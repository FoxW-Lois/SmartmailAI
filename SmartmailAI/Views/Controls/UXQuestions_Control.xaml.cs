using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SmartmailAI.ViewModels.Controls;

namespace SmartmailAI.Views.Controls;

public sealed partial class UXQuestions_Control : UserControl
{
	public UXQuestions_ViewModel ViewModel { get; }

	public UXQuestions_Control()
	{
		ViewModel = Ioc.Default.GetRequiredService<UXQuestions_ViewModel>();
		InitializeComponent();
	}

	private async void OnValidateClicked(object sender, RoutedEventArgs e)
	{
		await ViewModel.ValidateFormAsync();
	}
}
