using System.Threading.Tasks;

namespace SmartmailAI.Core.Contracts.Services.Addresses;

public interface ITokenStore
{
	Task<string?> GetRefreshTokenAsync(string tokenStorageKey, string _rootFolder);

	void DeleteToken(string tokenStorageKey, string _rootFolder);
}
