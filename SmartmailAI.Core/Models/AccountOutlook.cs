using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartmailAI.Core.Models;

public class AccountOutlook
{
	[Key][Column("Id")] public Guid Id { get; init; } = Guid.NewGuid();

	[Column("Email")][MaxLength(255)] public required string Email { get; init; }

	// ID unique Microsoft Entra / Azure AD
	[Column("MicrosoftUserId ")][MaxLength(255)] public required string MicrosoftUserId { get; init; }

	// Tenant Microsoft (important si multi-tenant)
	[Column("TenantId")][MaxLength(255)] public required string TenantId { get; init; }

	// Permet d’identifier le cache/token MSAL
	[Column("ConnectedAt")] public required DateTime ConnectedAt { get; init; }

	[Column("TokenStorageKey")][MaxLength(255)] public required string TokenStorageKey { get; init; }

	// Nécessaire à l'affichage en clair des Emails connectés dans le NavMenu
	public override string ToString() => Email;
}
