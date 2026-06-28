using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartmailAI.Core.Models.Security;

public class ManualLegitDomainsAndAddresses
{
	[Key][Column("Value")][MaxLength(255)] public required string Value { get; set; }

	[Column("IsDomain")] public required bool IsDomain { get; set; } // true = Domain | false = Address
	[Column("IsWhitelist")] public required bool IsWhitelist { get; set; } // true = Whitelist | false = Blacklist
}
