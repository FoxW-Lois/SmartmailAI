namespace SmartmailAI.Core.Models.Security;

public record DnsSecurityResult(
	string Domain,
	SpfStatus SpfStatus,
	string? SpfRecord,
	DmarcStatus DmarcStatus,
	string? DmarcRecord,
	bool IsSuspicious,
	string? Warning
);
