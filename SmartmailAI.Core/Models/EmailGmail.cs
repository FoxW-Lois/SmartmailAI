using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartmailAI.Core.Models;

public class EmailGmail
{
	[Key][Column("Id_internal")] public int Id_internal { get; init; } = default!;

	[Column("Guid")] public string Guid { get; init; } = default!;

	[Column("FromEmail")] public string FromEmail { get; init; } = default!;
	[Column("FromName")] public string? FromName { get; init; }
	[Column("ToEmail")] public string ToEmail { get; init; } = default!;
	[Column("ToName")] public string? ToName { get; init; }

	[Column("Subject")] public string Subject { get; init; } = default!;
	[Column("Body")] public string Body { get; init; } = default!;
	[Column("Date")] public DateTime? Date { get; init; }

	[Column("Owner")] public string Owner { get; init; } = default!;
	[Column("MailboxType")] public string MailboxType { get; init; } = default!;
}
