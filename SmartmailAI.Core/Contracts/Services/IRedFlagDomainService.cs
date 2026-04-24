using System.Threading.Tasks;

namespace SmartmailAI.Core.Contracts.Services;

public interface IRedFlagDomainService
{
	/// <summary>
	/// Retourne true si le domaine figure dans la liste red.flag.domains.
	/// </summary>
	Task<bool> IsFlaggedDomainAsync(string domain);

	/// <summary>
	/// Force le rechargement de la liste depuis la source distante.
	/// </summary>
	Task RefreshAsync();
}
