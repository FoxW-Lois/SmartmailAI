using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartmailAI.Core.Models;

public class Account
{
	[Key][Column("Id")] public int Id { get; set; }
	[Column("IndexGuid")] public required string IndexGuid { get; set; }

	[Column("Login")][MaxLength(100)] public required string Login { get; set; }
	[Column("PhoneNumber")][MaxLength(15)] public required string PhoneNumber { get; set; }
	[Column("Password")][MaxLength(255)] public required string Password { get; set; }
	[Column("Salt")][MaxLength(32)] public required string Salt { get; set; }
	[Column("EncryptedTotpSecret")] public string? EncryptedTotpSecret { get; set; }
	[Column("TwoFactorEnabled")] public required bool TwoFactorEnabled { get; set; }
	[Column("Enabled")] public required bool Enabled { get; set; }
	[Column("LastConnection")] public DateTime? LastConnection { get; set; }

	[Column("IsFirstConnection")] public bool IsFirstConnection { get; set; }
	[Column("NbOpenAppByWeek")] public int? NbOpenAppByWeek { get; set; }
	[Column("AverageDailyTrafic")] public string? AverageDailyTrafic { get; set; }
	[Column("RetrievedAllEmails")] public bool? RetrievedAllEmails { get; set; }
	[Column("DatePicked")] public DateOnly? DatePicked { get; set; }
}
