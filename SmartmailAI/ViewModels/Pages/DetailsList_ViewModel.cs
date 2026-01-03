using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartmailAI.Core.Models;

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

	// --- Commandes au clic droit ---

	[RelayCommand(CanExecute = nameof(CanMarkAsRead))]
	private async Task MarkAsReadAsync(Email email)
	{
		await _mailboxDataService.MarkEmailAsReadAsync(email);
	}

	private static bool CanMarkAsRead(Email? email) => email is not null && !email.IsRead;

	[RelayCommand(CanExecute = nameof(CanMarkAsUnread))]
	private async Task MarkAsUnreadAsync(Email email)
	{
		await _mailboxDataService.MarkEmailAsUnreadAsync(email);
	}

	private static bool CanMarkAsUnread(Email? email) => email is not null && email.IsRead;

	[RelayCommand]
	private async Task DeleteMailAsync(Email email)
	{
		if (email is null)
			return;

		if (email.MailboxType != MailboxType.Trash)
		{
			await _mailboxDataService.MarkEmailAsTrashedAsync(email);
			RefreshSelectedCategory();
			return;
		}

		await _mailboxDataService.DeleteEmailAsync(email);
	}

	[RelayCommand]
	private async Task TrashMailAsync(Email email)
	{
		if (email is null)
			return;

		await _mailboxDataService.MarkEmailAsTrashedAsync(email);
	}

	[RelayCommand]
	private async Task ArchiveMailAsync(Email email)
	{
		if (email is null)
			return;

		await _mailboxDataService.MarkEmailAsArchivedAsync(email);
	}

	[RelayCommand]
	private async Task MarkAsStarredAsync(Email email)
	{
		if (email is null)
			return;

		await _mailboxDataService.MarkEmailAsStarredAsync(email);
	}
}
