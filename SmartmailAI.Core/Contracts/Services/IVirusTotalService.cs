using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartmailAI.Core.Contracts.Services;

public record VirusTotalResult(
	string FileName,
	bool IsMalicious,
	int MaliciousCount,
	int TotalEngines,
	string? Permalink
);

public interface IVirusTotalService
{
	/// <summary>
	/// Analyse une liste de noms de fichiers (pièces jointes) via VirusTotal.
	/// Retourne les résultats pour chaque fichier connu de VirusTotal.
	/// </summary>
	Task<IReadOnlyList<VirusTotalResult>> AnalyzeAttachmentsAsync(IList<string> fileNames);
}
