namespace SmartmailAI.Core.Models.Security;

public record VirusTotalResult(
	string FileName,
	bool IsMalicious,
	int MaliciousCount,
	int TotalEngines,
	string? Permalink
);
