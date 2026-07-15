using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Windows.ApplicationModel.Resources;
using SmartmailAI.Core.Models.AI;
using SmartmailAI.Core.Models.Messengers;

namespace SmartmailAI.ViewModels.Pages;

public partial class AIinterface_ViewModel(I_AIService aiService) : ObservableObject
{
	private readonly I_AIService _aiService = aiService;
	private readonly ResourceLoader resourceLoader = new();

	public ObservableCollection<AIMessage> Conversation { get; } = [];

	[ObservableProperty]
	public partial string UserPrompt { get; set; } = string.Empty;

	[RelayCommand]
	private void Delete()
	{
		// Notifie EmailList_ViewModel de fermer l'AIinterfaceOverlay
		WeakReferenceMessenger.Default.Send(new RequestCloseIAinterfaceMessage());
		Reset(true);
	}

	[RelayCommand]
	private static void Discard()
	{
		// Notifie EmailList_ViewModel de fermer l'AIinterfaceOverlay
		WeakReferenceMessenger.Default.Send(new RequestCloseIAinterfaceMessage());
	}

	[RelayCommand]
	private static void Expand()
	{
		// Notifie EmailList_ViewModel d'ouvrir l'AIinterfaceOverlay en taille maximale
		WeakReferenceMessenger.Default.Send(new ToggleExpandIAinterfaceMessage());
	}

	[RelayCommand]
	private async Task SendPromptAsync()
	{
		string prompt = UserPrompt.Trim();

		if (string.IsNullOrWhiteSpace(prompt))
			return;

		Conversation.Add(new AIMessage()
		{
			Content = prompt,
			IsUser = true
		});

		object request = await _aiService.AIConversationAsync(Conversation);
		Reset();

		try
		{
			string answer = await _aiService.AIRequestAsync(request);

			Conversation.Add(new AIMessage()
			{
				Content = answer,
				IsUser = false
			});
		}
		catch (Exception)
		{
			// En cas d'échec de l'IA (indisponible ou erreur), on ignore silencieusement l'exception et on ajoute à la conversation
			// un message d'erreur

			Conversation.Add(new AIMessage()
			{
				Content = resourceLoader.GetString("Error_AI_Unavailable"),
				IsUser = false
			});
		}
	}

	private void Reset(bool resetMessageCollection = false)
	{
		UserPrompt = string.Empty;

		if (resetMessageCollection)
			Conversation.Clear();
	}
}
