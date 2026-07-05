using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Windows.ApplicationModel.Resources;
using SmartmailAI.Core.Contracts.Services.Addresses;
using SmartmailAI.Core.Models.AI;
using SmartmailAI.Core.Models.Messengers;

namespace SmartmailAI.ViewModels.Pages;

public partial class DetailsList_StandardViewModel(IMailReaderService mailReaderService, IAddressesService addressesService, I_AIService aiService,
	IDialogService dialogService) : ObservableRecipient
{
	private readonly IMailReaderService _mailReaderService = mailReaderService;
	private readonly IAddressesService _addressesService = addressesService;
	private readonly I_AIService _aiService = aiService;
	private readonly IDialogService _dialogService = dialogService;
	private readonly ResourceLoader resourceLoader = new();

	public ObservableCollection<AIMessage> Conversation { get; set; } = [];

	[ObservableProperty]
	private Email? currentEmail;

	[ObservableProperty]
	private string? currentEmailContentAIAssist;

	partial void OnCurrentEmailChanged(Email? value)
	{
		if (value is not null)
		{
			CurrentEmailContentAIAssist = null;
		}
	}

	[ObservableProperty]
	private bool hasAnAIResult = false;

	partial void OnCurrentEmailContentAIAssistChanged(string? value)
	{
		HasAnAIResult = !string.IsNullOrWhiteSpace(value);
	}

	[RelayCommand]
	private async Task SaveAttachmentAsync((string emailGuid, MailAttachment attachment, string destinationFolder) args)
	{
		// Désabonne d'abord si déjà enregistré
		WeakReferenceMessenger.Default.Unregister<ResponseAddressAccountMessage>(this);

		string resolvedAddress = string.Empty;

		// Demande l'adresse uniquement si l'utilisateur récupère une pièce jointe
		WeakReferenceMessenger.Default.Register<ResponseAddressAccountMessage>(this, (r, m) =>
		{
			resolvedAddress = m.AddressAccount;
			WeakReferenceMessenger.Default.Unregister<ResponseAddressAccountMessage>(this);
		});

		WeakReferenceMessenger.Default.Send(new RequestAddressAccountMessage());

		var account = await _addressesService.GetAccountByEmailAsync(resolvedAddress);

		if (account is null)
		{
			await _dialogService.ShowOneButtonDialogAsync(resourceLoader.GetString("Error_Title"),
				resourceLoader.GetString("Error_AccountUnfound_Gmail") + resourceLoader.GetString("Error_OrMessage") +
				resourceLoader.GetString("Error_CredentialsInvalidOrExpired_Gmail"));
			return;
		}

		try
		{
			await _mailReaderService.SaveAttachmentFromEmailAsync(args.emailGuid, args.attachment, args.destinationFolder, account);
		}
		catch (Exception)
		{
			await _dialogService.ShowOneButtonDialogAsync(resourceLoader.GetString("Error_Title"),
				resourceLoader.GetString("Error_SaveAttachmentFailed"));
			return;
		}
	}

	#region Commandes d'assistance IA sur les emails ouverts

	[RelayCommand]
	private async Task AITranslationAsync()
	{
		if (CurrentEmail is null || string.IsNullOrWhiteSpace(CurrentEmail.Content))
			return;

		Conversation = [];

		string prompt = resourceLoader.GetString("AI_Instruction_Translation") + "\n\n" + CurrentEmail?.Content;

		Conversation.Add(new AIMessage()
		{
			Content = prompt,
			IsUser = true
		});

		object request = await _aiService.AIConversationAsync(Conversation);

		try
		{
			string answer = await _aiService.AIRequestAsync(request);
			CurrentEmailContentAIAssist = answer;
		}
		catch (Exception)
		{
			// En cas d'échec de l'IA (indisponible ou erreur), on ignore silencieusement
		}
	}

	[RelayCommand]
	private async Task AISummarizeAsync()
	{
		if (CurrentEmail is null || string.IsNullOrWhiteSpace(CurrentEmail.Content))
			return;

		Conversation = [];

		string prompt = resourceLoader.GetString("AI_Instruction_Summarize") + "\n\n" + CurrentEmail?.Content;

		Conversation.Add(new AIMessage()
		{
			Content = prompt,
			IsUser = true
		});

		object request = await _aiService.AIConversationAsync(Conversation);

		try
		{
			string answer = await _aiService.AIRequestAsync(request);
			CurrentEmailContentAIAssist = answer;
		}
		catch (Exception)
		{
			// En cas d'échec de l'IA (indisponible ou erreur), on ignore silencieusement
		}
	}

	#endregion Commandes d'assistance IA sur les emails ouverts

	[RelayCommand]
	private async Task EditDraftedEmailAsync()
	{
		if (CurrentEmail is null)
			return;

		WeakReferenceMessenger.Default.Send(new OpenComposeMessage
		{
			Mode = ComposeMode.Edit,
			Guid = CurrentEmail.Guid,
			SenderEmail = CurrentEmail.SenderEmail,
			ReceiverEmail = CurrentEmail.ReceiverEmail,
			Subject = CurrentEmail.Subject,
			Body = CurrentEmail.Content
		});

		// Notifie DetailsList_ViewModel d'ouvrir le ComposeOverlay
		WeakReferenceMessenger.Default.Send(new RequestOpenOrCloseComposeMessage());
	}

	#region Réponse et Transfert

	[RelayCommand]
	private async Task ReplyAsync()
	{
		if (CurrentEmail is null)
			return;

		string? subject = $"Re: {CurrentEmail.Subject}";

		var body = $"""

			———— Message d'origine ————

			De : {CurrentEmail.SenderEmail}
			À : {CurrentEmail.ReceiverEmail}
			Date : {CurrentEmail.DateSent:g}
			Objet : {CurrentEmail.Subject}

			{CurrentEmail.Content}
			"""
		;

		WeakReferenceMessenger.Default.Send(new OpenComposeMessage
		{
			Mode = ComposeMode.Reply,
			SenderEmail = CurrentEmail.ReceiverEmail!,
			ReceiverEmail = CurrentEmail.SenderEmail,
			Subject = subject,
			Body = body
		});

		// Notifie DetailsList_ViewModel d'ouvrir le ComposeOverlay
		WeakReferenceMessenger.Default.Send(new RequestOpenOrCloseComposeMessage());
	}

	[RelayCommand]
	private async Task TransferAsync()
	{
		if (CurrentEmail is null)
			return;

		string? subject = $"FW: {CurrentEmail.Subject}";

		var body = $"""

			———— Message transféré ————

			De : {CurrentEmail.SenderEmail}
			À : {CurrentEmail.ReceiverEmail}
			Date : {CurrentEmail.DateSent:g}
			Objet : {CurrentEmail.Subject}

			{CurrentEmail.Content}
			"""
		;

		WeakReferenceMessenger.Default.Send(new OpenComposeMessage
		{
			Mode = ComposeMode.Forward,
			SenderEmail = CurrentEmail.ReceiverEmail!,
			Subject = subject,
			Body = body
		});

		// Notifie DetailsList_ViewModel d'ouvrir le ComposeOverlay
		WeakReferenceMessenger.Default.Send(new RequestOpenOrCloseComposeMessage());
	}

	#endregion Réponse et Transfert
}
