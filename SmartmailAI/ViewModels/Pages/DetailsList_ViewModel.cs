using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SmartmailAI.Core.Contracts.Repository;
using SmartmailAI.Core.Models.AI;
using SmartmailAI.Core.Models.Messengers;
using SmartmailAI.Core.Models.Security;
using Windows.ApplicationModel.Resources;

namespace SmartmailAI.ViewModels.Pages;

public partial class DetailsList_ViewModel : ObservableRecipient, INavigationAware
{
	private readonly IEmailsService _emailsService;
	private readonly IEmailRepository _emailRepository;
	private readonly IMLDA_Repository _mldaRepository;
	private readonly I_AIService _aiService;
	private readonly IDialogService _dialogService;
	private readonly ResourceLoader resourceLoader = new();

	public ObservableCollection<AIMessage> Conversation { get; set; } = [];

	public ObservableCollection<MailboxCategory> Categories { get; private set; } = [];

	// Stocke l'adresse Email sélectionnée pour la passer en tant qu'expéditeur à la fenêtre de composition
	private string addressAccount = string.Empty;

	[ObservableProperty]
	private MailboxCategory? selectedCategory;

	[ObservableProperty]
	private string? searchText;

	[ObservableProperty]
	private bool _isComposing = false;

	[ObservableProperty]
	private bool _isComposeExpanded = false;

	[ObservableProperty]
	private object? _selectedDetail;

	[ObservableProperty]
	private DateTimeOffset? _datePicked;

	[ObservableProperty]
	private bool _isDatePickerOpen = false;

	[ObservableProperty]
	private bool _isValideCategory = false;

	[ObservableProperty]
	private bool _isUnreadCategory = false;

	[ObservableProperty]
	private bool _isAIinterfaceVisible = false;

	[ObservableProperty]
	private bool _isAIinterfaceExpanded = false;

	// Pas de private/public car utilisé uniquement par la partial method
	partial void OnSearchTextChanged(string value) => RefreshSearchbarAsync(value);

	partial void OnDatePickedChanged(DateTimeOffset? value)
	{
		if (value is null)
			return;

		string formattedDate = value.Value.ToString("yyyy-MM-dd");

		SearchText += formattedDate;
	}

	partial void OnSelectedCategoryChanged(MailboxCategory? value)
	{
		if (value is null || SelectedCategory is null)
			return;

		IsValideCategory = SelectedCategory.MailboxType == MailboxType.Trash || SelectedCategory.MailboxType == MailboxType.PhishingSpam;
		IsUnreadCategory = SelectedCategory.MailboxType == MailboxType.Unread;
	}

	public DetailsList_ViewModel(IEmailsService emailsService, IEmailRepository emailRepository, IMLDA_Repository mldaRepository,
		I_AIService aiService, IDialogService dialogService)
	{
		_emailsService = emailsService;
		_emailRepository = emailRepository;
		_mldaRepository = mldaRepository;
		_aiService = aiService;
		_dialogService = dialogService;

		// Quand reçoit une demande, change la visibilité de la fenêtre de composition d'email
		WeakReferenceMessenger.Default.Register<RequestOpenOrCloseComposeMessage>(this, (r, m) =>
		{
			IsComposing = !IsComposing;
		});

		// Quand reçoit une demande, rafraîchit la liste des emails
		WeakReferenceMessenger.Default.Register<RequestRefreshEmailsMessage>(this, async (r, m) =>
		{
			await RefreshAllCategory();
		});

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

		// Quand reçoit une demande d'ouverture/fermeture de l'interface IA, change la visibilité de l'interface IA
		WeakReferenceMessenger.Default.Register<RequestCloseIAinterfaceMessage>(this, (r, m) =>
		{
			IsAIinterfaceVisible = !IsAIinterfaceVisible;
		});

		// Quand reçoit une demande de redimmentionnement de l'interface IA, change l'état d'expansion de l'interface IA
		WeakReferenceMessenger.Default.Register<ToggleExpandIAinterfaceMessage>(this, (_, _) =>
		{
			IsAIinterfaceExpanded = !IsAIinterfaceExpanded;
		});

		App.MainWindow.SizeChanged += (_, _) =>
		{
			OnPropertyChanged(nameof(HalfWindowWidth));
			OnPropertyChanged(nameof(ComposeMaxWidth));
			OnPropertyChanged(nameof(HalfWindowHeight));
			OnPropertyChanged(nameof(ComposeMaxHeight));

			OnPropertyChanged(nameof(AIinterfaceMaxWidth));
			OnPropertyChanged(nameof(AIinterfaceMaxHeight));
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

	private static double WindowHeight => App.MainWindow.Bounds.Height;
	private static double HalfWindowHeight => App.MainWindow.Bounds.Height / 1.95;

	public double ComposeMaxWidth => IsComposeExpanded ? WindowWidth * 0.65 : HalfWindowWidth;
	public double ComposeMaxHeight => IsComposeExpanded ? WindowHeight * 0.90 : HalfWindowHeight;

	partial void OnIsComposeExpandedChanged(bool value)
	{
		OnPropertyChanged(nameof(ComposeMaxWidth));
		OnPropertyChanged(nameof(ComposeMaxHeight));
	}

	#endregion Gestion de la taille du ComposeOverlay

	#region Gestion de la taille de l'AIinterfaceOverlay

	// Ne pas mettre AIinterfaceMaxWidth et AIinterfaceMaxHeight en static car utilisés dans le .xaml

	public double AIinterfaceMaxWidth => IsAIinterfaceExpanded ? WindowWidth * 0.65 : WindowWidth * 0.23;
	public double AIinterfaceMaxHeight => WindowHeight * 0.90;

	partial void OnIsAIinterfaceExpandedChanged(bool value)
	{
		OnPropertyChanged(nameof(AIinterfaceMaxWidth));
		OnPropertyChanged(nameof(AIinterfaceMaxHeight));
	}

	#endregion Gestion de la taille de l'AIinterfaceOverlay

	#region Commandes boutons interface

	[RelayCommand]
	private async Task OpenNewMailAsync()
	{
		IsComposing = !IsComposing;
		SelectedDetail = ComposeSentinel.Instance;

		// Passe l'email connecté en tant qu'expéditeur à la fenêtre de composition
		WeakReferenceMessenger.Default.Send(new OpenComposeMessage { Mode = ComposeMode.New, SenderEmail = addressAccount });
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

	[RelayCommand]
	private async Task DeleteAllMailsFromCurrentCategoryAsync()
	{
		if (SelectedCategory is null) return;

		var dialogResult = await _dialogService.ShowTwoButtonDialogAsync(resourceLoader.GetString("Dialog_Confirmation"),
			String.Concat(resourceLoader.GetString("Dialog_Delete_Confirm_part1"), SelectedCategory.MailboxType, resourceLoader.GetString("Dialog_Delete_Confirm_part2")),
			resourceLoader.GetString("Dialog_Agree"), resourceLoader.GetString("Dialog_Cancel"));

		if (dialogResult != WidgetDialogResult.Left)
			return;

		var emailList = SelectedCategory.Items;

		foreach (var item in emailList)
		{
			await DeleteItemAsync(item);
		}

		await RefreshAllCategory();
	}

	[RelayCommand]
	private async Task MarkAsReadAllMailsFromCurrentCategoryAsync()
	{
		if (SelectedCategory is null) return;

		var dialogResult = await _dialogService.ShowTwoButtonDialogAsync(resourceLoader.GetString("Dialog_Confirmation"),
			String.Concat(resourceLoader.GetString("Dialog_MarkAsRead_Confirm_part1"), SelectedCategory.MailboxType, resourceLoader.GetString("Dialog_MarkAsRead_Confirm_part2")),
			resourceLoader.GetString("Dialog_Agree"), resourceLoader.GetString("Dialog_Cancel"));

		if (dialogResult != WidgetDialogResult.Left)
			return;

		var emailList = SelectedCategory.Items;

		foreach (var item in emailList)
		{
			await _emailsService.MarkEmailAsReadAsync(item); ;
		}

		await RefreshAllCategory();
	}

	#endregion Commandes boutons interface

	#region Commandes d'assistance IA

	// Affiche/masque l'interface de l'assistant IA
	[RelayCommand]
	private async Task SubmitAIAsync()
	{
		IsAIinterfaceVisible = !IsAIinterfaceVisible;
	}

	[RelayCommand]
	private async Task AI_FilterAsync()
	{
		string? prompt = await _dialogService.ShowTwoButtonDialogWithRichEditboxAsync(resourceLoader.GetString("AI_Title_Search"),
			resourceLoader.GetString("Search_instructions"), resourceLoader.GetString("AI_Button_Research")) ?? null;

		if (string.IsNullOrWhiteSpace(prompt))
			return;

		Conversation = [];

		Conversation.Add(new AIMessage()
		{
			Content = prompt,
			IsUser = true
		});

		object request = await _aiService.AIConversationAsync(Conversation);

		try
		{
			SearchText = await _aiService.AIRequestAsync(request);
		}
		catch (Exception)
		{
			// En cas d'échec de l'IA (indisponible ou erreur), on ignore silencieusement
		}
	}

	#endregion Commandes d'assistance IA

	#region Commandes de filtrage

	[RelayCommand]
	private async Task FilterAsync()
	{
	}

	[RelayCommand]
	private async Task DateBeforeFilterAsync()
	{
		IsDatePickerOpen = true;

		if (SearchText is not null && SearchText.Length > 0)
			SearchText += " ";
		SearchText += "Date:Before:";
	}

	[RelayCommand]
	private async Task DateAfterFilterAsync()
	{
		IsDatePickerOpen = true;

		if (SearchText is not null && SearchText.Length > 0)
			SearchText += " ";
		SearchText += "Date:After:";
	}

	[RelayCommand]
	private async Task AttachmentYesFilterAsync()
	{
		if (SearchText is not null && SearchText.Length > 0)
			SearchText += " ";
		SearchText += "Attachment:Yes";
	}

	[RelayCommand]
	private async Task AttachmentNoFilterAsync()
	{
		if (SearchText is not null && SearchText.Length > 0)
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

	private static bool CanRestore(Email? email) => email is not null && email.MailboxType is MailboxType.Trash or MailboxType.Archives;

	[RelayCommand]
	private async Task DeleteMailAsync(Email email)
	{
		var previousMailboxType = email.MailboxType;

		await DeleteItemAsync(email);

		var newMailboxType = email.MailboxType;
		await RefreshSelectedCategory(previousMailboxType, newMailboxType);
	}

	private async Task DeleteItemAsync(Email email)
	{
		if (email is null)
			return;

		if (email.MailboxType != MailboxType.Trash)
			await _emailsService.MarkEmailAsTrashedAsync(email);
		else
			await _emailsService.DeleteEmailAsync(email);
	}

	[RelayCommand(CanExecute = nameof(CanMoveToPhishingSpam))]
	private async Task MoveMailToPhishingSpamAsync(Email email)
	{
		if (email is null)
			return;

		var previousMailboxType = email.MailboxType;
		await _emailsService.MarkEmailAsPhishingSpamAsync(email);

		var newMailboxType = email.MailboxType;
		await RefreshSelectedCategory(previousMailboxType, newMailboxType);

		var emailDecrypted = await _emailRepository.DecryptDataAsync(email);
		await UpdateMLDAlist(emailDecrypted.SenderEmail, false, false);
	}

	private static bool CanMoveToPhishingSpam(Email? email) => email is not null &&
		!(email.MailboxType is MailboxType.Trash or MailboxType.Archives or MailboxType.Drafts or MailboxType.PhishingSpam);

	[RelayCommand(CanExecute = nameof(CanRemoveFromPhishingSpam))]
	private async Task RemoveMailFromPhishingSpam(Email email)
	{
		if (email is null)
			return;

		var previousMailboxType = email.MailboxType;
		await _emailsService.MarkEmailAsNotPhishingSpamAsync(email);

		var newMailboxType = email.MailboxType;
		await RefreshSelectedCategory(previousMailboxType, newMailboxType);

		var emailDecrypted = await _emailRepository.DecryptDataAsync(email);
		await UpdateMLDAlist(emailDecrypted.SenderEmail, false, true);
	}

	private static bool CanRemoveFromPhishingSpam(Email? email) => email is not null && email.MailboxType == MailboxType.PhishingSpam;

	private async Task UpdateMLDAlist(string senderEmail, bool isDomain, bool isWhitelist)
	{
		ManualLegitDomainsAndAddresses? mlda = new()
		{
			Value = senderEmail,
			IsDomain = isDomain,
			IsWhitelist = isWhitelist
		};

		if (await _mldaRepository.MLDAExistsAsync(senderEmail))
			await _mldaRepository.UpdateMLDA_Async(mlda);
		else
			await _mldaRepository.AddMLDA_Async(mlda);
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
		if (previousMailboxType is null || newMailboxType is null)
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
