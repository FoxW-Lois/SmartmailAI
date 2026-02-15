using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Contracts.Services.Addresses;

public interface IGmailCredentialService
{
	Task<UserCredential> ConnectAsync(string userKey);

	Task<UserCredential?> GetCredentialAsync(AccountGmail accountGmail);
}
