using System.Threading.Tasks;

namespace SmartmailAI.Core.Contracts.Services.Addresses;

public interface IOtherTokenStore
{
	Task SavePasswordAsync(string key, string password);

	Task<string?> GetPasswordAsync(string key);

	void DeleteToken(string key);
}
