using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using SmartmailAI.Core.Contracts.Services.Addresses;

namespace SmartmailAI.Core.Services.Addresses;

public class GmailApiService : IGmailApiService
{
	public async Task<string> GetEmailAddressAsync(UserCredential credential)
	{
		var service = new GmailService(new BaseClientService.Initializer
		{
			HttpClientInitializer = credential,
			ApplicationName = "MailOAuthTester"
		});

		var profile = await service.Users.GetProfile("me").ExecuteAsync();
		return profile.EmailAddress;
	}
}
