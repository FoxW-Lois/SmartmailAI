namespace SmartmailAI.Core.Models;

public class AddOtherAddressRequest
{
	public required string Email { get; init; }
	public required string UserName { get; init; }
	public required string Password { get; init; }

	#region IMAP

	public required string ImapHost { get; init; }
	public required int ImapPort { get; init; }
	public bool ImapUseSsl { get; init; } = true;

	#endregion IMAP

	#region SMTP

	public required string SmtpHost { get; init; }
	public required int SmtpPort { get; init; }
	public bool SmtpUseSsl { get; init; } = true;

	#endregion SMTP
}
