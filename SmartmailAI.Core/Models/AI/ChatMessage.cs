using System.Text.Json.Serialization;

namespace SmartmailAI.Core.Models.AI;

public class ChatMessage
{
	[JsonPropertyName("role")]
	public string Role { get; set; } = ""; // "user", "assistant", "system"

	[JsonPropertyName("content")]
	public string Content { get; set; } = "";
}
