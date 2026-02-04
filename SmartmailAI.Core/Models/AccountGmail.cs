using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartmailAI.Core.Models;

public class AccountGmail
{
	[Key][Column("Id")] public Guid Id { get; init; } = Guid.NewGuid();

	[Column("Email")][MaxLength(255)] public required string Email { get; init; }
	[Column("GoogleUserId")] public required string GoogleUserId { get; init; }
	[Column("ConnectedAt")] public required DateTime ConnectedAt { get; init; }
	[Column("TokenStorageKey")] public required string TokenStorageKey { get; init; }
}
