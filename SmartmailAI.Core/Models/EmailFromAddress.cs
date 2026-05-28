using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace SmartmailAI.Core.Models;

public class EmailFromAddress
{
	// TODO : A voir
	// [Column("Guid")] public string Guid { get; init; } = default!;
	[Key][Column("Guid")] public string Guid { get; init; } = default!;

	[Column("FromEmail")] public string FromEmail { get; init; } = default!;
	[Column("FromName")] public string? FromName { get; init; }
	[Column("ToEmail")] public string ToEmail { get; init; } = default!;
	[Column("ToName")] public string? ToName { get; init; }
	[Column("Cc")] public string? Cc { get; init; }
	[Column("Bcc")] public string? Bcc { get; init; }

	[Column("Subject")] public string Subject { get; init; } = default!;
	[Column("Body")] public string Body { get; init; } = default!;
	[Column("Date")] public DateTime? Date { get; init; }

	[Column("Attachments")]
	public string AttachmentsSerialized
	{
		get => JsonSerializer.Serialize(Attachments);
		set => Attachments = string.IsNullOrEmpty(value)
			? []
			: JsonSerializer.Deserialize<List<MailAttachment>>(value) ?? [];
	}

	[NotMapped] public List<MailAttachment> Attachments { get; set; } = [];

	[Column("Owner")] public string Owner { get; init; } = default!;
	[Column("MailboxType")] public string MailboxType { get; init; } = default!;
}
