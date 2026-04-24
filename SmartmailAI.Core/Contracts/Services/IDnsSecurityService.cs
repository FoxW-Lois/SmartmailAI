using System.Threading.Tasks;

namespace SmartmailAI.Core.Contracts.Services;

public enum SpfStatus
{
	Unknown,   // Pas de record SPF trouvé
	Pass,      // Record SPF présent et valide
	Fail,      // Record SPF présent mais restrictif (domaine non autorisé)
	SoftFail,  // Record SPF en ~all (permissif)
	None,      // Aucun enregistrement SPF
}

public enum DmarcStatus
{
	Unknown,   // Pas de record DMARC trouvé
	Present,   // Record DMARC présent (politique définie)
	None,      // Aucun enregistrement DMARC
}

public record DnsSecurityResult(
	string Domain,
	SpfStatus  SpfStatus,
	string?    SpfRecord,
	DmarcStatus DmarcStatus,
	string?    DmarcRecord,
	bool       IsSuspicious,
	string?    Warning
);

public interface IDnsSecurityService
{
	/// <summary>
	/// Vérifie les enregistrements SPF et DMARC du domaine expéditeur.
	/// </summary>
	Task<DnsSecurityResult> CheckDomainAsync(string senderEmail);
}
