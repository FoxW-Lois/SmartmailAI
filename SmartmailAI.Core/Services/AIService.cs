using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SmartmailAI.Core.Contracts.Services;
using SmartmailAI.Core.Models.AI;

namespace SmartmailAI.Core.Services;

public class AIService : I_AIService
{
	private readonly string prompt_system_path = Path.Combine(AppContext.BaseDirectory, "Prompt-system-Smartmail.txt");
	private readonly HttpClient client = new();

	public async Task<object> AIConversationAsync(ObservableCollection<AIMessage> Conversation)
	{
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

		return request;
	}

	public async Task<string> AIRequestAsync(object request)
	{
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
			return "";
		}

		var chat = JsonSerializer.Deserialize<ChatResponse>(result);
		string answer = chat?.Choices[0].Message.Content ?? "Une erreur est survenue.";

		return answer;
	}
}
