using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Util.Store;
using SmartmailAI.Core.Contracts.Repository;
using SmartmailAI.Core.Contracts.Services.Addresses;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Services.Addresses;

public class GmailCredentialService(IAddressesRepository addressesRepository) : IGmailCredentialService
{
	private readonly IAddressesRepository _addressesRepository = addressesRepository;

	private static readonly FileDataStore? dataStore = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
		"SmartmailAI", "Google.Apis.AuthToken"), true);

	public async Task<UserCredential?> ConnectAsync(string userKey)
	{
		try
		{
			var secrets = new ClientSecrets
			{
				ClientId = "687689133134-p1h6di4c2chv5dne4rfi3cfljp0ln9n8.apps.googleusercontent.com",
				ClientSecret = "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"
			};

			var scopes = new[] { GmailService.Scope.GmailReadonly, GmailService.Scope.GmailSend };

			return await GoogleWebAuthorizationBroker.AuthorizeAsync(
				secrets, scopes, userKey, CancellationToken.None, dataStore
			);
		}
		catch (Exception)
		{
			// Erreurs déjà gérées dans la méthode appelante
			return null;
		}
	}

	public async Task<UserCredential?> GetCredentialAsync(AccountGmail accountGmail)
	{
		try
		{
			var secrets = new ClientSecrets
			{
				ClientId = "687689133134-p1h6di4c2chv5dne4rfi3cfljp0ln9n8.apps.googleusercontent.com",
				ClientSecret = "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"
			};

			var scopes = new[] { GmailService.Scope.GmailReadonly, GmailService.Scope.GmailSend };

			var decryptedAccountGmail = await _addressesRepository.DecryptDataAsync(accountGmail);

			return await GoogleWebAuthorizationBroker.AuthorizeAsync(
				secrets, scopes, decryptedAccountGmail.TokenStorageKey, CancellationToken.None, dataStore
			);
		}
		catch (Exception)
		{
			// Erreurs déjà gérées dans les méthodes appelantes
			return null;
		}
	}
}
