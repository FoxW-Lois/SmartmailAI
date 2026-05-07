using System.Threading.Tasks;
using Microsoft.Identity.Client;
using SmartmailAI.Core.Contracts.Services.Addresses;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Services.Addresses;

public class OutlookLogoutService(OutlookTokenStore tokenStore) : IOutlookLogoutService
{
	private static readonly IPublicClientApplication MsalApp = OutlookCredentialService.MsalApp; // Partage la même instance

	public async Task LogoutAsync(AccountOutlook account)
	{
		var storedId = tokenStore.GetAccountId(account.TokenStorageKey);
		if (storedId is not null)
		{
			var knownAccount = await MsalApp.GetAccountAsync(storedId);
			if (knownAccount is not null)
				await MsalApp.RemoveAsync(knownAccount); // Révoque le token côté MSAL
		}

		tokenStore.DeleteToken(account.TokenStorageKey);
	}
}
