using System.Collections.Generic;
using System.Threading.Tasks;
using SmartmailAI.Core.Models.Security;

namespace SmartmailAI.Core.Contracts.Services.Security;

public interface IVirusTotalService
{
	// Analyse une liste de noms de fichiers (pièces jointes) via VirusTotal.
	// Retourne les résultats pour chaque fichier connu de VirusTotal.
	Task<IReadOnlyList<VirusTotalResult>> AnalyzeAttachmentsAsync(IList<string> fileNames);
}
