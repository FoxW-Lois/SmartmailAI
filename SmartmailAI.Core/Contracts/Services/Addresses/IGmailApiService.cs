using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;

namespace SmartmailAI.Core.Contracts.Services.Addresses;

public interface IGmailApiService
{
	Task<string> GetEmailAddressAsync(UserCredential credential);
}
