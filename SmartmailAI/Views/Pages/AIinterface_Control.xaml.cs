using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace SmartmailAI.Views.Pages;

public sealed partial class AIinterface_Control : UserControl
{
	public AIinterface_ViewModel ViewModel { get; }

	public AIinterface_Control()
	{
		ViewModel = Ioc.Default.GetRequiredService<AIinterface_ViewModel>();
		InitializeComponent();
	}
}
