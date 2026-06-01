using System.ComponentModel.DataAnnotations.Schema;

namespace SmartmailAI.Core.Models;

public class AccountGmail : AccountMailBase
{
	[Column("GoogleUserId")] public required string GoogleUserId { get; set; }
}
