using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartmailAI.Core.Models;

public class AccountMailBase
{
	[Key][Column("Id")] public Guid Id { get; init; } = Guid.NewGuid();
	[Column("IndexGuidHash")] public required string IndexGuidHash { get; init; }

	[Column("Email")][MaxLength(255)] public required string Email { get; set; }
	[Column("ConnectedAt")] public required DateTime ConnectedAt { get; init; }
	[Column("IsFirstConnection")] public bool IsFirstConnection { get; set; }

	// Clé locale de stockage
	[Column("TokenStorageKey")] public required string TokenStorageKey { get; set; }

	// Nécessaire à l'affichage en clair des Emails connectés dans le NavMenu
	public override string ToString() => Email;
}
