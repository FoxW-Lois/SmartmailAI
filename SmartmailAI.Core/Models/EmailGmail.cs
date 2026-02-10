using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartmailAI.Core.Models;

public class EmailGmail
{
	[Key][Column("Id_internal")] public int Id_internal { get; init; } = default!;

	[Column("Guid")] public string Guid { get; init; } = default!;

	[Column("From")] public string? From { get; init; }
	[Column("To")] public string? To { get; init; }
	[Column("Subject")] public string Subject { get; init; } = default!;
	[Column("Body")] public string Body { get; init; } = default!;
	[Column("Date")] public DateTime? Date { get; init; }

	[Column("Owner")] public string Owner { get; init; } = default!;
	[Column("MailboxType")] public string MailboxType { get; init; } = default!;
}
