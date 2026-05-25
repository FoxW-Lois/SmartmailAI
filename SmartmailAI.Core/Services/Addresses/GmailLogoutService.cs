using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using SmartmailAI.Core.Contracts.Services.Addresses;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Services.Addresses;

public class GmailLogoutService(ITokenStore tokenStore, HttpClient httpClient) : IGmailLogoutService
{
	private readonly ITokenStore _tokenStore = tokenStore;
	private readonly HttpClient _httpClient = httpClient;

	private readonly string _rootFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
		"SmartmailAI", "Google.Apis.AuthToken");

	public async Task LogoutAsync(AccountGmail account)
	{
		var refreshToken = await _tokenStore.GetRefreshTokenAsync(account.TokenStorageKey, _rootFolder);

		if (!string.IsNullOrWhiteSpace(refreshToken))
		{
			await _httpClient.PostAsync(
				"https://oauth2.googleapis.com/revoke",
				new FormUrlEncodedContent(
				[
					new("token", refreshToken)
				])
			);
		}

		_tokenStore.DeleteToken(account.TokenStorageKey, _rootFolder);
	}
}
