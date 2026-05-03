using System.Threading.Tasks;

namespace SmartmailAI.Core.Contracts.Services.Security;

public interface IRedFlagDomainService
{
	// Retourne true si le domaine figure dans la liste red.flag.domains.
	Task<bool> IsFlaggedDomainAsync(string domain);

	// Force le rechargement de la liste depuis la source distante.
	Task RefreshAsync();
}
