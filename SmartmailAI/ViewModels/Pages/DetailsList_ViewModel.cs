using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SmartmailAI.Core.Models.Messengers;

namespace SmartmailAI.ViewModels.Pages;

public partial class DetailsList_ViewModel : ObservableRecipient, INavigationAware
{
	private readonly IEmailsService _emailsService;

	public ObservableCollection<MailboxCategory> Categories { get; private set; } = [];

	// Stocke l'adresse Email sélectionnée pour la passer en tant qu'expéditeur à la fenêtre de composition
	private string addressAccount = string.Empty;

	[ObservableProperty]
	private MailboxCategory? selectedCategory;

	[ObservableProperty]
	private string? searchText;

	[ObservableProperty]
	private bool _isComposing;

	[ObservableProperty]
	private bool _isComposeExpanded;

	[ObservableProperty]
	private object? _selectedDetail;

	// Pas de private/public car utilisé uniquement par la partial method
	partial void OnSearchTextChanged(string value) => RefreshSearchbarAsync(value);

	public DetailsList_ViewModel(IEmailsService emailsService)
	{
		_emailsService = emailsService;

		WeakReferenceMessenger.Default.Register<CloseComposeMessage>(this, (r, m) => { IsComposing = false; });

		// Quand reçoit une demande (ouverture des détails d'un email), envoi l'email connecté à la fenêtre des détails
		WeakReferenceMessenger.Default.Register<RequestAddressAccountMessage>(this, (r, m) =>
		{
			WeakReferenceMessenger.Default.Send(new ResponseAddressAccountMessage { AddressAccount = addressAccount });
		});

		// Quand reçoit une demande de redimmentionnement du compose, change l'état d'expansion du compose
		WeakReferenceMessenger.Default.Register<ToggleExpandComposeMessage>(this, (_, _) =>
		{
			IsComposeExpanded = !IsComposeExpanded;
		});

		App.MainWindow.SizeChanged += (_, _) =>
		{
			OnPropertyChanged(nameof(HalfWindowWidth));
			OnPropertyChanged(nameof(ComposeMaxWidth));
		};
	}

	public async Task OnNavigatedTo(object? parameter)
	{
		IEnumerable<MailboxCategory>? categoriesWithEmails;

		if (parameter is not string paramAddressAccount)
		{
			categoriesWithEmails = await _emailsService.GetAllCategoriesAsync();
		}
		else
		{
			addressAccount = paramAddressAccount;
			categoriesWithEmails = await _emailsService.GetAllCategoriesAsync(addressAccount);
		}

		Categories.Clear();

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

	// Appelé quand l'utilisateur clique sur le bouton "Nouveau"
	partial void OnSelectedDetailChanged(object? value)
	{
		if (value is not ComposeSentinel)
			IsComposing = false;
	}

	#region Gestion de la taille du ComposeOverlay

	private static double WindowWidth => App.MainWindow.Bounds.Width;
	private static double HalfWindowWidth => App.MainWindow.Bounds.Width / 2.5;

	public double ComposeMaxWidth => IsComposeExpanded ? WindowWidth * 0.65 : HalfWindowWidth;

	partial void OnIsComposeExpandedChanged(bool value)
	{
		OnPropertyChanged(nameof(ComposeMaxWidth));
	}

	#endregion Gestion de la taille du ComposeOverlay

	[RelayCommand]
	private async Task OpenNewMailAsync()
	{
		IsComposing = !IsComposing;
		SelectedDetail = ComposeSentinel.Instance;

		// Passe l'email connecté en tant qu'expéditeur à la fenêtre de composition
		WeakReferenceMessenger.Default.Send(new OpenComposeMessage { SenderEmail = addressAccount });
	}

	[RelayCommand]
	private async Task CloseNewMailAsync()
	{
		IsComposing = false;
		SelectedDetail = null;
	}

	[RelayCommand]
	private async Task RefreshEmailListAsync()
	{
		await RefreshAllCategory();
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
		await RefreshSelectedCategory(previousMailboxType, newMailboxType, email);
	}

	[RelayCommand(CanExecute = nameof(CanMarkAsRead))]
	private async Task MarkAsReadAsync(Email email)
	{
		var previousMailboxType = email.MailboxType;
		await _emailsService.MarkEmailAsReadAsync(email);

		var newMailboxType = MailboxType.Unread;
		await RefreshSelectedCategory(previousMailboxType, newMailboxType, email);
	}

	private static bool CanMarkAsRead(Email? email) => email is not null && !email.IsRead;

	[RelayCommand(CanExecute = nameof(CanMarkAsUnread))]
	private async Task MarkAsUnreadAsync(Email email)
	{
		var previousMailboxType = email.MailboxType;
		await _emailsService.MarkEmailAsUnreadAsync(email);

		var newMailboxType = MailboxType.Unread;
		await RefreshSelectedCategory(previousMailboxType, newMailboxType);
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
		await RefreshSelectedCategory(previousMailboxType, newMailboxType);
	}

	[RelayCommand(CanExecute = nameof(CanRestore))]
	private async Task RestoreMailAsync(Email email)
	{
		var previousMailboxType = email.MailboxType;
		await _emailsService.RestoreEmailAsync(email);

		var newMailboxType = email.MailboxType;
		await RefreshSelectedCategory(previousMailboxType, newMailboxType);
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
		await RefreshSelectedCategory(previousMailboxType, newMailboxType);
	}

	#endregion Commandes au clic droit

	#region Méthodes de refresh

	// Event écoutant la demande de restauration de la sélection d'un email après le refresh de la liste
	public event Action<Email>? RestoreSelectionRequested;

	// --- Méthode pour Refresh la liste des emails affichés lors du clique sur le bouton de refresh ---
	private async Task RefreshAllCategory()
	{
		var mailboxTypesToRefresh = ComputeMailboxTypesToRefresh(null, null, null);

		await FetchAndApplyEmailsAsync(mailboxTypesToRefresh);
	}

	// --- Méthode pour Refresh la liste des emails affichés lors des actions clic droit ---
	// emailToRestore : Email sur lequel l'action a été effectuée
	private async Task RefreshSelectedCategory(MailboxType previousMailboxType, MailboxType newMailboxType, Email? emailToRestore = null)
	{
		if (SelectedCategory is null) return;

		// Onglet actuellement ouvert (ancien emplacement du mail)
		var previousCategory = SelectedCategory;

		var mailboxTypesToRefresh = ComputeMailboxTypesToRefresh(previousMailboxType, newMailboxType, emailToRestore);

		await FetchAndApplyEmailsAsync(mailboxTypesToRefresh);

		await TryRestoreSelectionAsync(previousCategory, emailToRestore);
	}

	// Calcul des catégories à rafraîchir
	private HashSet<MailboxType> ComputeMailboxTypesToRefresh(MailboxType? previousMailboxType = null, MailboxType? newMailboxType = null,
		Email? email = null)
	{
		HashSet<MailboxType> types = [];

		// Si un des 2 paramètres MailboxType est null, on rafraîchit toutes les catégories
		if (previousMailboxType == null || newMailboxType == null)
		{
			foreach (var mailboxType in Enum.GetValues<MailboxType>())
			{
				types.Add(mailboxType);
			}

			return types;
		}

		// On part toujours des deux catégories directement impliquées par l'action
		// + AllMails est toujours concerné : tout changement d'état d'un email l'impacte
		types = [(MailboxType)previousMailboxType, (MailboxType)newMailboxType, MailboxType.AllMails];

		if (email is null) return types;

		// Si l'email est marqué comme non-lu, on ajoute à rafraîchir la catégorie Unread
		if (!email.IsRead)
			types.Add(MailboxType.Unread);

		// Si on est dans AllMails, on ajoute la catégorie "naturelle" de l'email (Inbox, Sent, etc...)
		if (SelectedCategory?.MailboxType == MailboxType.AllMails)
			types.Add(email.MailboxType);

		return types;
	}

	// Rafraîchissement de toutes les catégories concernées en parallèle
	private async Task FetchAndApplyEmailsAsync(IEnumerable<MailboxType> mailboxTypes)
	{
		var fetchTasks = mailboxTypes.Select(async mailboxType =>
		{
			var emails = await _emailsService.GetEmailsByMailboxTypeAsync(mailboxType, addressAccount);
			return (mailboxType, emails);
		});

		var results = await Task.WhenAll(fetchTasks);

		foreach (var (mailboxType, emails) in results)
		{
			var category = Categories.FirstOrDefault(c => c.MailboxType == mailboxType);
			if (category is null) continue;

			category.ItemsCollection.Clear();
			foreach (var email in emails)
				category.ItemsCollection.Add(email);
		}
	}

	// Restaure la sélection après le cycle de rendu UI
	private Task TryRestoreSelectionAsync(MailboxCategory previousCategory, Email? emailToRestore)
	{
		if (emailToRestore is null) return Task.CompletedTask;

		var emailToRestoreFound = previousCategory.ItemsCollection.FirstOrDefault(e => e.Guid == emailToRestore.Guid && e.IsRead);

		if (emailToRestoreFound is not null)
			RestoreSelectionRequested?.Invoke(emailToRestoreFound);

		return Task.CompletedTask;
	}

	// --- Méthode pour Refresh la liste des emails affichés quand la barre de recherches est utilisée ---
	private async void RefreshSearchbarAsync(string researchValue)
	{
		if (SelectedCategory is null) return;

		var refreshedEmails = await _emailsService.GetEmailsByMailboxTypeAsync(SelectedCategory.MailboxType, addressAccount);

		// Recharge les données sans casser le binding
		SelectedCategory.ReplaceAllItems(refreshedEmails);

		// Applique le filtre sur la collection observable
		SelectedCategory.ApplyFilter(researchValue);
	}

	#endregion Méthodes de refresh
}
