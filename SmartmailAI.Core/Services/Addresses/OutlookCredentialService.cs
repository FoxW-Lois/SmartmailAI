using System.Threading.Tasks;
using Microsoft.Identity.Client;
using SmartmailAI.Core.Contracts.Services.Addresses;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Services.Addresses;

public class OutlookCredentialService(OutlookTokenStore tokenStore) : IOutlookCredentialService
{
	// TODO : A remplacer par l'Application (client) ID Azure
	private const string ClientId = "VOTRE_CLIENT_ID_AZURE";

	private const string Authority = "https://login.microsoftonline.com/common";

	private static readonly string[] Scopes =
	[
		"https://graph.microsoft.com/Mail.Read",
		"https://graph.microsoft.com/Mail.Send",
		"https://graph.microsoft.com/User.Read"
	];

	// L'instance MSAL doit être partagée pour bénéficier du cache en mémoire
	internal static readonly IPublicClientApplication MsalApp =
		PublicClientApplicationBuilder
			.Create(ClientId)
			.WithAuthority(Authority)
			.WithRedirectUri("https://login.microsoftonline.com/common/oauth2/nativeclient")
			.Build();

	// Ouvre le navigateur pour une connexion interactive (premier ajout de compte).
	public async Task<AuthenticationResult?> ConnectAsync()
	{
		try
		{
			var result = await MsalApp.AcquireTokenInteractive(Scopes).ExecuteAsync();

			// Persiste l'identifiant du compte pour les reconnexions silencieuses
			tokenStore.SaveAccountId(result.Account.HomeAccountId.Identifier, result.Account.HomeAccountId.Identifier);
			return result;
		}
		catch (MsalException)
		{
			return null;
		}
	}

	// Tente une reconnexion silencieuse (refresh token). Ouvre le navigateur en fallback.
	public async Task<AuthenticationResult?> GetCredentialAsync(AccountOutlook account)
	{
		try
		{
			var storedId = tokenStore.GetAccountId(account.TokenStorageKey);
			if (storedId is null) return null;

			var knownAccount = await MsalApp.GetAccountAsync(storedId);
			if (knownAccount is null) return null;

			// Tentative silencieuse (utilise le refresh token en cache)
			return await MsalApp.AcquireTokenSilent(Scopes, knownAccount).ExecuteAsync();
		}
		catch (MsalUiRequiredException)
		{
			// Le refresh token a expiré → on relance une auth interactive
			return await ConnectAsync();
		}
		catch (MsalException)
		{
			return null;
		}
	}
}
