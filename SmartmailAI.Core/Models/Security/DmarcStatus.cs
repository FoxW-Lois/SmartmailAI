namespace SmartmailAI.Core.Models.Security;

public enum DmarcStatus
{
	Unknown,    // Pas de record DMARC trouvé
	Present,    // Record DMARC présent (politique définie)
	None,       // Aucun enregistrement DMARC
}
