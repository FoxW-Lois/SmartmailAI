using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2.Responses;
using SmartmailAI.Core.Contracts.Services.Addresses;

namespace SmartmailAI.Core.Services.Addresses;

public class GmailTokenStore : ITokenStore
{
	public async Task<string?> GetRefreshTokenAsync(string tokenStorageKey)
	{
		var rootDir = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
			"Google.Apis.Auth"
		);

		if (!Directory.Exists(rootDir))
			return null;

		var tokenFile = Directory
			.GetFiles(rootDir, $"*{tokenStorageKey}*", SearchOption.TopDirectoryOnly)
			.FirstOrDefault();

		if (tokenFile is null)
			return null;

		var json = await File.ReadAllTextAsync(tokenFile);
		var token = JsonSerializer.Deserialize<TokenResponse>(json);

		return token?.RefreshToken;
	}

	public void DeleteToken(string tokenStorageKey)
	{
		var rootDir = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
			"Google.Apis.Auth"
		);

		if (!Directory.Exists(rootDir))
			return;

		var tokenFiles = Directory.GetFiles(rootDir, $"*{tokenStorageKey}*", SearchOption.TopDirectoryOnly);

		foreach (var file in tokenFiles)
		{
			File.Delete(file);
		}
	}
}
