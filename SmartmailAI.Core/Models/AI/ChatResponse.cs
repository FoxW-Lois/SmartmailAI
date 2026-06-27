using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SmartmailAI.Core.Models.AI;

public class ChatResponse
{
	[JsonPropertyName("choices")]
	public List<Choice> Choices { get; set; } = [];
}
