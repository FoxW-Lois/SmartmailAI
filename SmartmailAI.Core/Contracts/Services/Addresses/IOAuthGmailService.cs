using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;

namespace SmartmailAI.Core.Contracts.Services.Addresses;

public interface IOAuthGmailService
{
	Task<UserCredential> ConnectAsync(string userKey);
}

