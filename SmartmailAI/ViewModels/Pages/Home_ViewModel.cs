using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using SmartmailAI.Core.Models.Messengers;

namespace SmartmailAI.ViewModels.Pages;

public partial class Home_ViewModel : ObservableRecipient
{
	[ObservableProperty]
	public partial string AppDisplayName { get; set; } = ConstantHelper.AppDisplayName;

	[ObservableProperty]
	public partial bool IsUXQuestionsVisible { get; set; } = false;

	private readonly IAccountService _accountService;

	public Home_ViewModel(IAccountService accountService)
	{
		_accountService = accountService;

		// Quand reçoit une demande, cache le UserControl UXQuestions
		WeakReferenceMessenger.Default.Register<RequestUpdateUXQuestionsMessage>(this, async (r, m) =>
		{
			IsUXQuestionsVisible = false;
		});
	}

	public async Task InitializeAsync()
	{
		var account = await _accountService.GetAccountByLoginInLocalSessionAsync();

		if (account == null)
			return;

		if (account.IsFirstConnection is false)
		{
			IsUXQuestionsVisible = false;
			return;
		}

		IsUXQuestionsVisible = true;
	}
}
