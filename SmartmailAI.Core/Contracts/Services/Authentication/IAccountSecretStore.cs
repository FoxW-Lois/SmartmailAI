using System.Threading.Tasks;

namespace SmartmailAI.Core.Contracts.Services.Authentication;

public interface IAccountSecretStore
{
	Task SaveSecretAsync(string login, string encryptedSecret);

	Task<string?> GetSecretAsync(string login);

	Task DeleteSecretAsync(string login);
}
