namespace SmartmailAI.Core.Models.Messengers;

public class OpenComposeMessage
{
	public ComposeMode Mode { get; init; }

	public string SenderEmail { get; init; } = string.Empty;

	public string? ReceiverEmail { get; init; } = string.Empty;

	public string? Subject { get; set; }

	public string? Body { get; set; }
}
