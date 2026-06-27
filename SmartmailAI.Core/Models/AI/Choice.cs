using System.Text.Json.Serialization;

namespace SmartmailAI.Core.Models.AI;

public class Choice
{
	[JsonPropertyName("message")]
	public ChatMessage Message { get; set; } = default!;
}
