namespace SmartmailAI.Core.Models;

public class MailAttachment
{
	public string FileName { get; set; }
	public string FilePath { get; set; }        // Pour les pièces jointes à envoyer
	public string AttachmentId { get; set; }    // Pour les pièces jointes reçues (Gmail) TODO: voir pour Outlook et STMP/IMAP
	public string MimeType { get; set; }
	public ulong FileSize { get; set; }
}
