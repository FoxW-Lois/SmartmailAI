using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartmailAI.Core.Models;

public class AccountOther : AccountMailBase
{
	[Column("UserName")][MaxLength(255)] public required string UserName { get; init; }
	[Column("Password")][MaxLength(255)] public required string Password { get; set; }

	#region Propriétés IMAP

	[Column("ImapHost")][MaxLength(255)] public required string ImapHost { get; init; }
	[Column("ImapPort")] public required int ImapPort { get; init; }
	[Column("ImapUseSsl")] public bool ImapUseSsl { get; init; } = true;

	#endregion Propriétés IMAP

	#region Propriétés SMTP

	[Column("SmtpHost")][MaxLength(255)] public required string SmtpHost { get; init; }
	[Column("SmtpPort")] public required int SmtpPort { get; init; }
	[Column("SmtpUseSsl")] public bool SmtpUseSsl { get; init; } = true;

	#endregion Propriétés SMTP
}
