using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using SmartmailAI.Core.Contracts.Services.Addresses;

namespace SmartmailAI.Core.Services.Addresses;

public class OAuthGmailService : IOAuthGmailService
{
	public async Task<UserCredential> ConnectAsync(string userKey)
	{
		var secrets = new ClientSecrets
		{
			ClientId = "687689133134-p1h6di4c2chv5dne4rfi3cfljp0ln9n8.apps.googleusercontent.com",
			ClientSecret = "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"
		};

		var scopes = new[] { GmailService.Scope.GmailReadonly };

		return await GoogleWebAuthorizationBroker.AuthorizeAsync(
			secrets, scopes, userKey, CancellationToken.None
		);
	}
}
