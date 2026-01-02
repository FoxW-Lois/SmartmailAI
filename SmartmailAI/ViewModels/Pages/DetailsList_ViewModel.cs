using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SmartmailAI.ViewModels.Pages;

public partial class DetailsList_ViewModel : ObservableRecipient, INavigationAware
{
	private readonly IMailboxDataService _mailboxDataService;

	[ObservableProperty]
	private MailboxCategory? selectedCategory;

	public ObservableCollection<MailboxCategory> Categories { get; private set; } = [];

	public DetailsList_ViewModel(IMailboxDataService mailboxDataService)
	{
		_mailboxDataService = mailboxDataService;
	}

	public async Task OnNavigatedTo(object parameter)
	{
		Categories.Clear();

		var categories = await _mailboxDataService.GetAllCategoriesAsync();

		foreach (var category in categories)
		{
			Categories.Add(category);
		}
	}

	public void OnNavigatedFrom()
	{
	}

	public void EnsureItemSelected()
	{
		SelectedCategory ??= Categories.FirstOrDefault();
	}
}
