using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace SmartmailAI.ViewModels.Pages;

public partial class DetailsList_ViewModel : ObservableRecipient, INavigationAware
{
	private readonly IEmailsService _emailsService;

	public ObservableCollection<MailboxCategory> Categories { get; private set; } = [];

	[ObservableProperty]
	private MailboxCategory? selectedCategory;

	[ObservableProperty]
	private string? searchText;

	[ObservableProperty]
	private bool _isComposing;

	[ObservableProperty]
	private object? _selectedDetail;

	// Pas de private/public car utilisé uniquement par la partial method
	partial void OnSearchTextChanged(string value) => RefreshSearchbarAsync(value);

	public DetailsList_ViewModel(IEmailsService emailsService)
	{
		_emailsService = emailsService;

		WeakReferenceMessenger.Default.Register<CloseComposeMessage>(this, (r, m) =>
		{
			IsComposing = false;
		});
	}

	public async Task OnNavigatedTo(object? parameter)
	{
		// TODO: Si besoin d'utiliser des données statiques, commenter le bloc conditionnel ↓
		if (parameter is not string addressAccount)
			return;

		Categories.Clear();

		var categoriesWithEmails = await _emailsService.GetAllCategoriesAsync(/*addressAccount*/);

		foreach (var category in categoriesWithEmails)
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

	// Appelé quand l'utilisateur sélectionne un email normal
	partial void OnSelectedDetailChanged(object? value)
	{
		if (value is not ComposeSentinel)
			IsComposing = false;
	}

	[RelayCommand]
	private async Task OpenNewMailAsync()
	{
		IsComposing = !IsComposing;
		SelectedDetail = ComposeSentinel.Instance;
	}

	[RelayCommand]
	private async Task CloseNewMailAsync()
	{
		IsComposing = false;
		SelectedDetail = null;
	}

	#region Commandes de filtrage

	[RelayCommand]
	private async Task FilterAsync()
	{
	}

	[RelayCommand]
	private async Task DateBeforeFilterAsync()
	{
		if (SearchText != null && SearchText.Length > 0)
			SearchText += " ";
		SearchText += "Date:Before:";
	}

	[RelayCommand]
	private async Task DateAfterFilterAsync()
	{
		if (SearchText != null && SearchText.Length > 0)
			SearchText += " ";
		SearchText += "Date:After:";
	}

	[RelayCommand]
	private async Task AttachmentYesFilterAsync()
	{
		if (SearchText != null && SearchText.Length > 0)
			SearchText += " ";
		SearchText += "Attachment:Yes";
	}

	[RelayCommand]
	private async Task AttachmentNoFilterAsync()
	{
		if (SearchText != null && SearchText.Length > 0)
			SearchText += " ";
		SearchText += "Attachment:No";
	}

	#endregion Commandes de filtrage

	#region Commandes au clic droit

	[RelayCommand]
	private async Task MarkAsStarredAsync(Email email)
	{
		if (email is null)
			return;

		var previousMailboxType = email.MailboxType;
		await _emailsService.MarkEmailAsStarredAsync(email);

		var newMailboxType = MailboxType.Starred;
		RefreshSelectedCategory(previousMailboxType, newMailboxType);
	}

	[RelayCommand(CanExecute = nameof(CanMarkAsRead))]
	private async Task MarkAsReadAsync(Email email)
	{
		var previousMailboxType = email.MailboxType;
		await _emailsService.MarkEmailAsReadAsync(email);

		var newMailboxType = MailboxType.Unread;
		RefreshSelectedCategory(previousMailboxType, newMailboxType);
	}

	private static bool CanMarkAsRead(Email? email) => email is not null && !email.IsRead;

	[RelayCommand(CanExecute = nameof(CanMarkAsUnread))]
	private async Task MarkAsUnreadAsync(Email email)
	{
		var previousMailboxType = email.MailboxType;
		await _emailsService.MarkEmailAsUnreadAsync(email);

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
		await _emailsService.MarkEmailAsArchivedAsync(email);

		var newMailboxType = email.MailboxType;
		RefreshSelectedCategory(previousMailboxType, newMailboxType);
	}

	[RelayCommand(CanExecute = nameof(CanRestore))]
	private async Task RestoreMailAsync(Email email)
	{
		var previousMailboxType = email.MailboxType;
		await _emailsService.RestoreEmailAsync(email);

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
			await _emailsService.MarkEmailAsTrashedAsync(email);
		else
			await _emailsService.DeleteEmailAsync(email);

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
		var previousRefreshedEmails = await _emailsService.GetEmailsByMailboxTypeAsync(previousMailboxType);

		// Refresh UI
		previousCategory.ItemsCollection.Clear();
		foreach (var email in previousRefreshedEmails)
			previousCategory.ItemsCollection.Add(email);

		// --- Refresh de l'onglet devenant le nouvel emplacement du mail ---
		var newCategory = Categories.FirstOrDefault(c => c.MailboxType == newMailboxType);

		var newRefreshedEmails = await _emailsService.GetEmailsByMailboxTypeAsync(newMailboxType);

		newCategory.ItemsCollection.Clear();
		foreach (var email in newRefreshedEmails)
			newCategory.ItemsCollection.Add(email);
	}

	// --- Méthode pour Refresh la liste des emails affichés quand la barre de recherches est utilisée ---
	private async void RefreshSearchbarAsync(string researchValue)
	{
		if (SelectedCategory is null)
			return;

		var refreshedEmails = await _emailsService.GetEmailsByMailboxTypeAsync(SelectedCategory.MailboxType);

		// Recharge les données sans casser le binding
		SelectedCategory.ReplaceAllItems(refreshedEmails);

		// Applique le filtre sur la collection observable
		SelectedCategory.ApplyFilter(researchValue, SelectedCategory.MailboxType);
	}

	#endregion Méthodes de refresh
}
