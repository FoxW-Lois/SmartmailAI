using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Windows.ApplicationModel.Resources;
using SmartmailAI.Core.Contracts.Repository;
using SmartmailAI.Core.Contracts.Services.Addresses;
using SmartmailAI.Core.Models.AI;
using SmartmailAI.Core.Models.Messengers;
using Windows.Storage.Pickers;

namespace SmartmailAI.ViewModels.Pages;

public partial class DetailsList_NewMailViewModel : ObservableObject
{
	private readonly IAddressesService _addressesService;
	private readonly IGmailApiService _gmailApiService;
	private readonly IGmailCredentialService _gmailCredentialService;
	private readonly IOtherProtocolService _otherProtocolService;
	private readonly IOtherCredentialService _otherCredentialService;
	private readonly IEmailRepository _emailsRepository;
	private readonly IEmailsService _emailsService;
	private readonly IOtherTokenStore _otherTokenStore;
	private readonly I_AIService _aiService;
	private readonly IDialogService _dialogService;
	private readonly ResourceLoader resourceLoader = new();

	public ObservableCollection<AIMessage> Conversation { get; set; } = [];
	private string? userInstructions { get; set; } = null;

	public ObservableCollection<MailAttachment> Attachments { get; } = [];
	public bool HasAttachments => Attachments.Count > 0;

	public DetailsList_NewMailViewModel(IAddressesService addressesService, IGmailApiService gmailApiService, IGmailCredentialService gmailCredentialService,
		IOtherProtocolService otherProtocolService, IOtherCredentialService otherCredentialService, IOtherTokenStore otherTokenStore,
	IEmailRepository emailsRepository, IEmailsService emailsService, I_AIService aiService, IDialogService dialogService)
	{
		_addressesService = addressesService;
		_gmailApiService = gmailApiService;
		_gmailCredentialService = gmailCredentialService;
		_otherProtocolService = otherProtocolService;
		_otherCredentialService = otherCredentialService;
		_emailsRepository = emailsRepository;
		_emailsService = emailsService;
		_otherTokenStore = otherTokenStore;
		_aiService = aiService;
		_dialogService = dialogService;

		WeakReferenceMessenger.Default.Register<OpenComposeMessage>(this, (r, m) =>
		{
			ComposeMode = m.Mode;
			_guid = m.Guid;
			_from = m.SenderEmail;
			To = m.ReceiverEmail ?? string.Empty;
			Subject = m.Subject ?? string.Empty;
			Body = m.Body ?? string.Empty;
			_emailOwner = m.EmailOwner;
		});

		Attachments.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HasAttachments));
	}

	#region Emails properties

	[ObservableProperty]
	public partial ComposeMode ComposeMode { get; set; }

	private string? _guid, _emailOwner;

	private string _from = string.Empty;

	[ObservableProperty]
	public partial string To { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string Cc { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string Bcc { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string Subject { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string Body { get; set; } = string.Empty;

	[ObservableProperty]
	public partial bool IsCcVisible { get; set; } = false;

	[ObservableProperty]
	public partial bool IsBccVisible { get; set; } = false;

	#endregion Emails properties

	public bool IsSubjectEnable => ComposeMode is ComposeMode.New or ComposeMode.Edit;

	partial void OnComposeModeChanged(ComposeMode value)
	{
		OnPropertyChanged(nameof(IsSubjectEnable));
	}

	[RelayCommand]
	private async Task SendAsync()
	{
		if (string.IsNullOrWhiteSpace(To) || string.IsNullOrWhiteSpace(Subject))
			return;

		AccountMailBase? account;

		if (_emailOwner is not null && ComposeMode is ComposeMode.Forward or ComposeMode.Reply or ComposeMode.ReplyAll && _from != _emailOwner)
			_from = _emailOwner;

		account = await _addressesService.GetAccountByEmailAsync(_from);

		if (account is null)
		{
			await ShowErrorAsync("Error_AccountUnfound_Email");
			return;
		}

		if (!IsCcVisible)
			Cc = string.Empty;

		if (!IsBccVisible)
			Bcc = string.Empty;

		try
		{
			switch (account)
			{
				case AccountGmail gmailAccount:
					await SendWithGmailAsync(gmailAccount);
					break;

				case AccountOther otherAccount:
					await SendWithOtherAsync(otherAccount);
					break;

				default:
					return;
			}
			// TODO: ajouter un check account is AccountOutlook accountOutlook

			// Notifie DetailsList_ViewModel de fermer le ComposeOverlay
			Discard();

			if (_guid is not null)
				await _emailsRepository.DeleteEmailByGuidAsync(_guid);
		}
		catch (Exception)
		{
			await ShowErrorAsync("Error_EmailSendingFailed");
		}
	}

	#region Sedding emails helpers

	private async Task SendWithGmailAsync(AccountGmail account)
	{
		var credential = await _gmailCredentialService.GetCredentialAsync(account, false);

		if (credential is null)
		{
			await ShowErrorAsync("Error_AccountUnfound_Email");
			return;
		}

		await _gmailApiService.SendEmailAsync(credential, MailAddressParserHelper.ParseStringAddresses(To), Subject, Body, Attachments,
			MailAddressParserHelper.ParseStringAddresses(Cc), MailAddressParserHelper.ParseStringAddresses(Bcc));
	}

	private async Task SendWithOtherAsync(AccountOther account)
	{
		var connected = await PrepareOtherAccountAsync(account);

		if (!connected)
		{
			await ShowErrorAsync("Error_AccountUnfound_Other");
			return;
		}

		await _otherProtocolService.SendEmailAsync(account, MailAddressParserHelper.ParseStringAddresses(To), Subject, Body, Attachments,
			MailAddressParserHelper.ParseStringAddresses(Cc), MailAddressParserHelper.ParseStringAddresses(Bcc));
	}

	#endregion Sedding emails helpers

	#region Other account helpers

	private async Task<bool> PrepareOtherAccountAsync(AccountOther account)
	{
		string? password = await _otherTokenStore.GetPasswordAsync(account.TokenStorageKey);

		if (password is null)
			return false;

		account.Password = password;

		return await _otherCredentialService.ConnectAsync(account);
	}

	#endregion Other account helpers

	private async Task ShowErrorAsync(string resourceKey)
	{
		await _dialogService.ShowOneButtonDialogAsync(resourceLoader.GetString("Error_Title"), resourceLoader.GetString(resourceKey));
	}

	#region Commandes gérant l'état de la fenêtre de composition

	[RelayCommand]
	private void Discard()
	{
		// Notifie DetailsList_ViewModel de fermer le ComposeOverlay
		WeakReferenceMessenger.Default.Send(new RequestOpenOrCloseComposeMessage());
		Reset();
	}

	[RelayCommand]
	private async Task DraftedAsync()
	{
		// Récupère le contenu de tous les champs, puis les enregistre en base dans un objet Email avec la catégorie "Drafts"
		await _emailsService.ScribbleEmailAsync(_guid, _from, To, Subject, Body, Cc, Bcc);

		// Notifie DetailsList_ViewModel de fermer le ComposeOverlay
		WeakReferenceMessenger.Default.Send(new RequestOpenOrCloseComposeMessage());
		Reset();

		// Notifie DetailsList_ViewModel de refresh la liste des emails
		WeakReferenceMessenger.Default.Send(new RequestRefreshEmailsMessage());
	}

	[RelayCommand]
	private static void Expand()
	{
		// Notifie DetailsList_ViewModel d'ouvrir le ComposeOverlay en taille maximale
		WeakReferenceMessenger.Default.Send(new ToggleExpandComposeMessage());
	}

	[RelayCommand]
	private void ToggleCc()
	{
		IsCcVisible = !IsCcVisible;
	}

	[RelayCommand]
	private void ToggleBcc()
	{
		IsBccVisible = !IsBccVisible;
	}

	private void Reset()
	{
		To = string.Empty;
		Cc = string.Empty;
		Bcc = string.Empty;
		Subject = string.Empty;
		Body = string.Empty;
		IsCcVisible = false;
		IsBccVisible = false;
	}

	#endregion Commandes gérant l'état de la fenêtre de composition

	#region Commandes d'assistance IA (écriture d'email)

	[RelayCommand]
	private async Task AIWritingAsync()
	{
		userInstructions = await _dialogService.ShowTwoButtonDialogWithRichEditboxAsync(resourceLoader.GetString("AI_Title_Writing"),
			resourceLoader.GetString("Writing_instructions"), resourceLoader.GetString("AI_Button_Writing")) ?? null;

		if (string.IsNullOrWhiteSpace(userInstructions))
			return;

		Conversation = [];

		string prompt = resourceLoader.GetString("AI_Instruction_Writing") + "\n\n" + userInstructions;

		Conversation.Add(new AIMessage()
		{
			Content = prompt,
			IsUser = true
		});

		object request = await _aiService.AIConversationAsync(Conversation);

		try
		{
			string answer = await _aiService.AIRequestAsync(request);
			Body = answer;
		}
		catch (Exception)
		{
			// En cas d'échec de l'IA (indisponible ou erreur), on ignore silencieusement
		}
	}

	[RelayCommand]
	private async Task AITranslationAsync()
	{
		if (string.IsNullOrWhiteSpace(Body))
			return;

		userInstructions = await _dialogService.ShowTwoButtonDialogWithTextboxAsync(resourceLoader.GetString("AI_Title_Translation"),
			resourceLoader.GetString("Preferred_language"), resourceLoader.GetString("AI_Button_Translate")) ?? null;

		if (string.IsNullOrWhiteSpace(userInstructions))
			return;

		Conversation = [];

		string prompt = resourceLoader.GetString("AI_Instruction_Translation") + userInstructions + ":\n\n" + Body;

		Conversation.Add(new AIMessage()
		{
			Content = prompt,
			IsUser = true
		});

		object request = await _aiService.AIConversationAsync(Conversation);

		try
		{
			string answer = await _aiService.AIRequestAsync(request);
			Body = answer;
		}
		catch (Exception)
		{
			// En cas d'échec de l'IA (indisponible ou erreur), on ignore silencieusement
		}
	}

	[RelayCommand]
	private async Task AIRephrasingAsync()
	{
		if (string.IsNullOrWhiteSpace(Body))
			return;

		Conversation = [];

		string prompt = resourceLoader.GetString("AI_Instruction_Rephrasing") + "\n\n" + Body;

		Conversation.Add(new AIMessage()
		{
			Content = prompt,
			IsUser = true
		});

		object request = await _aiService.AIConversationAsync(Conversation);

		try
		{
			string answer = await _aiService.AIRequestAsync(request);
			Body = answer;
		}
		catch (Exception)
		{
			// En cas d'échec de l'IA (indisponible ou erreur), on ignore silencieusement
		}
	}

	[RelayCommand]
	private async Task AICorrectionAsync()
	{
		if (string.IsNullOrWhiteSpace(Body))
			return;

		Conversation = [];

		string prompt = resourceLoader.GetString("AI_Instruction_Correction") + "\n\n" + Body;

		Conversation.Add(new AIMessage()
		{
			Content = prompt,
			IsUser = true
		});

		object request = await _aiService.AIConversationAsync(Conversation);

		try
		{
			string answer = await _aiService.AIRequestAsync(request);
			Body = answer;
		}
		catch (Exception)
		{
			// En cas d'échec de l'IA (indisponible ou erreur), on ignore silencieusement
		}
	}

	#endregion Commandes d'assistance IA (écriture d'email)

	#region Commandes de rédaction d'email

	[RelayCommand]
	private async Task AttachFileAsync()
	{
		var picker = new FileOpenPicker();
		picker.FileTypeFilter.Add("*");

		// Nécessaire en WinUI3 pour associer le picker à la fenêtre
		var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
		WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

		var files = await picker.PickMultipleFilesAsync();
		foreach (var file in files)
			AddAttachment(file.Path, file.Name);
	}

	[RelayCommand]
	private void RemoveAttachment(MailAttachment attachment)
	{
		Attachments.Remove(attachment);
	}

	private void AddAttachment(string path, string name)
	{
		if (Attachments.Any(a => a.FilePath == path))
			return;

		Attachments.Add(new MailAttachment
		{
			FileName = name,
			FilePath = path
		});
	}

	#endregion Commandes de rédaction d'email
}
