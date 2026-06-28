using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SmartmailAI.Core.Models.AI;
using SmartmailAI.Core.Models.Messengers;

namespace SmartmailAI.ViewModels.Pages;

public partial class AIinterface_ViewModel : ObservableObject
{
	private readonly string prompt_system_path = Path.Combine(AppContext.BaseDirectory, "Prompt-system-Smartmail.txt");
	private readonly HttpClient client = new();

	public ObservableCollection<AIMessage> Conversation { get; } = [];

	[ObservableProperty]
	private string _userPrompt = string.Empty;

	[RelayCommand]
	private void Delete()
	{
		// Notifie DetailsList_ViewModel de fermer l'AIinterfaceOverlay
		WeakReferenceMessenger.Default.Send(new RequestCloseIAinterfaceMessage());
		Reset(true);
	}

	[RelayCommand]
	private static void Discard()
	{
		// Notifie DetailsList_ViewModel de fermer l'AIinterfaceOverlay
		WeakReferenceMessenger.Default.Send(new RequestCloseIAinterfaceMessage());
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

		Conversation.Add(new AIMessage()
		{
			Content = prompt,
			IsUser = true
		});

		#region AI interaction

		var request = new
		{
			model = "mistralai/ministral-3-3b",
			messages = Conversation.Select(m => new
			{
				role = m.IsUser ? "user" : "assistant",
				content = m.Content
			})
			.Prepend(new
			{
				role = "system",
				content = await File.ReadAllTextAsync(prompt_system_path)
			}),
			temperature = 0.7
		};

		Reset();

		string json = JsonSerializer.Serialize(request);

		var response = await client.PostAsync(
			"http://localhost:1234/v1/chat/completions",
			new StringContent(json, Encoding.UTF8, "application/json"
		));

		string result = await response.Content.ReadAsStringAsync();

		if (!response.IsSuccessStatusCode)
		{
			string error = await response.Content.ReadAsStringAsync();
			Debug.WriteLine(error);
			return;
		}

		var chat = JsonSerializer.Deserialize<ChatResponse>(result);
		string answer = chat?.Choices[0].Message.Content ?? "Une erreur est survenue.";

		#endregion AI interaction

		Conversation.Add(new AIMessage()
		{
			Content = answer,
			IsUser = false
		});
	}

	private void Reset(bool resetMessageCollection = false)
	{
		UserPrompt = string.Empty;

		if (resetMessageCollection)
			Conversation.Clear();
	}
}
