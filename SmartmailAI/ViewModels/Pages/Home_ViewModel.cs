using CommunityToolkit.Mvvm.ComponentModel;

namespace SmartmailAI.ViewModels.Pages;

public partial class Home_ViewModel : ObservableRecipient
{
	[ObservableProperty]
	public partial string AppDisplayName { get; set; } = ConstantHelper.AppDisplayName;

	public Home_ViewModel()
	{
	}
}
