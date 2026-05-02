namespace SmartmailAI.Core.Models.Security;

public enum SpfStatus
{
	Unknown,    // Pas de record SPF trouvé
	Pass,       // Record SPF présent et valide
	Fail,       // Record SPF présent mais restrictif (domaine non autorisé)
	SoftFail,   // Record SPF en ~all (permissif)
	None,       // Aucun enregistrement SPF
}
