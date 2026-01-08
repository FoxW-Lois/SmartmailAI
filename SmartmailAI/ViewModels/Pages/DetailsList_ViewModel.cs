using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SmartmailAI.ViewModels.Pages;

public partial class DetailsList_ViewModel : ObservableRecipient, INavigationAware
{
	private readonly IMailboxDataService _mailboxDataService;

	[ObservableProperty]
	private MailboxCategory? selectedCategory;

	public ObservableCollection<MailboxCategory> Categories { get; private set; } = [];

	[ObservableProperty]
	private string searchText;

	// Pas de private/public car utilisé uniquement par la partial method
	partial void OnSearchTextChanged(string value) => RefreshSearchbarAsync(value);

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

	[RelayCommand]
	private async Task FilterAsync()
	{
	}

	[RelayCommand]
	private async Task DateFilterAsync()
	{
		if (SearchText != null)
			SearchText += "  ";
		SearchText += "Date:";
	}

	[RelayCommand]
	private async Task AttachmentFilterAsync()
	{
		if (SearchText != null)
			SearchText += "  ";
		SearchText += "Attachment:";
	}

	#region Commandes au clic droit

	[RelayCommand]
	private async Task MarkAsStarredAsync(Email email)
	{
		if (email is null)
			return;

		var previousMailboxType = email.MailboxType;
		await _mailboxDataService.MarkEmailAsStarredAsync(email);

		var newMailboxType = MailboxType.Starred;
		RefreshSelectedCategory(previousMailboxType, newMailboxType);
	}

	[RelayCommand(CanExecute = nameof(CanMarkAsRead))]
	private async Task MarkAsReadAsync(Email email)
	{
		var previousMailboxType = email.MailboxType;
		await _mailboxDataService.MarkEmailAsReadAsync(email);

		var newMailboxType = MailboxType.Unread;
		RefreshSelectedCategory(previousMailboxType, newMailboxType);
	}

	private static bool CanMarkAsRead(Email? email) => email is not null && !email.IsRead;

	[RelayCommand(CanExecute = nameof(CanMarkAsUnread))]
	private async Task MarkAsUnreadAsync(Email email)
	{
		var previousMailboxType = email.MailboxType;
		await _mailboxDataService.MarkEmailAsUnreadAsync(email);

		var newMailboxType = MailboxType.Unread;
		RefreshSelectedCategory(previousMailboxType, newMailboxType);
	}

	private static bool CanMarkAsUnread(Email? email) => email is not null && email.IsRead;

	[RelayCommand]
	private async Task ArchiveMailAsync(Email email)
	{
		if (email is null)
			return;

		var previousMailboxType = email.MailboxType;
		await _mailboxDataService.MarkEmailAsArchivedAsync(email);

		var newMailboxType = email.MailboxType;
		RefreshSelectedCategory(previousMailboxType, newMailboxType);
	}

	[RelayCommand(CanExecute = nameof(CanRestore))]
	private async Task RestoreMailAsync(Email email)
	{
		var previousMailboxType = email.MailboxType;
		await _mailboxDataService.RestoreEmailAsync(email);

		var newMailboxType = email.MailboxType;
		RefreshSelectedCategory(previousMailboxType, newMailboxType);
	}

	private static bool CanRestore(Email? email) => email is not null && (email.MailboxType == MailboxType.Trash || email.MailboxType == MailboxType.Archives);

	[RelayCommand]
	private async Task DeleteMailAsync(Email email)
	{
		if (email is null)
			return;

		var previousMailboxType = email.MailboxType;

		if (email.MailboxType != MailboxType.Trash)
			await _mailboxDataService.MarkEmailAsTrashedAsync(email);
		else
			await _mailboxDataService.DeleteEmailAsync(email);

		var newMailboxType = email.MailboxType;
		RefreshSelectedCategory(previousMailboxType, newMailboxType);
	}

	#endregion Commandes au clic droit

	#region Méthodes de refresh

	// --- Méthode pour Refresh la liste des emails affichés lors des actions clic droit ---
	private async void RefreshSelectedCategory(MailboxType previousMailboxType, MailboxType newMailboxType)
	{
		if (SelectedCategory is null)
			return;

		// --- Refresh de l'onglet actuellement ouvert (ancien emplacement du mail) ---
		var previousCategory = SelectedCategory;

		// Recharge les mails depuis le service
		var previousRefreshedEmails = await _mailboxDataService.GetEmailsByMailboxTypeAsync(previousMailboxType);

		// Refresh UI
		previousCategory.ItemsCollection.Clear();
		foreach (var email in previousRefreshedEmails)
			previousCategory.ItemsCollection.Add(email);

		// --- Refresh de l'onglet devenant le nouvel emplacement du mail ---
		var newCategory = Categories.FirstOrDefault(c => c.MailboxType == newMailboxType);

		var newRefreshedEmails = await _mailboxDataService.GetEmailsByMailboxTypeAsync(newMailboxType);

		newCategory.ItemsCollection.Clear();
		foreach (var email in newRefreshedEmails)
			newCategory.ItemsCollection.Add(email);
	}

	// --- Méthode pour Refresh la liste des emails affichés quand la barre de recherches est utilisée ---
	private async void RefreshSearchbarAsync(string researchValue)
	{
		if (SelectedCategory is null)
			return;

		var refreshedEmails = await _mailboxDataService.GetEmailsByMailboxTypeAsync(SelectedCategory.MailboxType);

		// Recharge les données sans casser le binding
		SelectedCategory.ReplaceAllItems(refreshedEmails);

		// Applique le filtre sur la collection observable
		SelectedCategory.ApplyFilter(researchValue);
	}

	#endregion Méthodes de refresh
}
