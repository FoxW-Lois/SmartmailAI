using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SmartmailAI.Core.Models.Messengers;

namespace SmartmailAI.ViewModels.Pages;

public partial class AIinterface_ViewModel : ObservableObject
{
	public ObservableCollection<AIMessage> Messages { get; } = [];

	[ObservableProperty]
	private string _userPrompt = string.Empty;

	[RelayCommand]
	private void Discard()
	{
		// Notifie DetailsList_ViewModel de fermer l'AIinterfaceOverlay
		WeakReferenceMessenger.Default.Send(new RequestCloseIAinterfaceMessage());
		Reset(true);
	}

	[RelayCommand]
	private static void Expand()
	{
		// Notifie DetailsList_ViewModel d'ouvrir l'AIinterfaceOverlay en taille maximale
		WeakReferenceMessenger.Default.Send(new ToggleExpandIAinterfaceMessage());
	}

	[RelayCommand]
	private async Task SendPromptAsync()
	{
		string prompt = UserPrompt.Trim();

		if (string.IsNullOrWhiteSpace(prompt))
			return;

		Messages.Add(new AIMessage()
		{
			Content = prompt,
			IsUser = true
		});

		Messages.Add(new AIMessage()
		{
			Content = "✨✨ Salut c'est Maily ! ✨✨",
			IsUser = false
		});

		Reset();
	}

	private void Reset(bool resetMessageCollection = false)
	{
		UserPrompt = string.Empty;

		if (resetMessageCollection)
			Messages.Clear();
	}
}
