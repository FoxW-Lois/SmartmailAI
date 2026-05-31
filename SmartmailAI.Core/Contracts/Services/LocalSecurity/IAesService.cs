using System.Threading.Tasks;

namespace SmartmailAI.Core.Contracts.Services.LocalSecurity;

public interface IAesService
{
	Task<string> EncryptAsync(string plainText);

	Task<string> DecryptAsync(string cipherText);
}
