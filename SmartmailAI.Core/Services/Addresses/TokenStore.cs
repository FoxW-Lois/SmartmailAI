using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2.Responses;
using SmartmailAI.Core.Contracts.Services.Addresses;

namespace SmartmailAI.Core.Services.Addresses;

public class TokenStore : ITokenStore
{
	public async Task<string?> GetRefreshTokenAsync(string tokenStorageKey, string _rootFolder)
	{
		if (!Directory.Exists(_rootFolder))
			return null;

		var tokenFile = Directory.GetFiles(_rootFolder, $"*{tokenStorageKey}*", SearchOption.TopDirectoryOnly).FirstOrDefault();

		if (tokenFile is null)
			return null;

		var json = await File.ReadAllTextAsync(tokenFile);
		var token = JsonSerializer.Deserialize<TokenResponse>(json);

		return token?.RefreshToken;
	}

	public void DeleteToken(string tokenStorageKey, string _rootFolder)
	{
		if (!Directory.Exists(_rootFolder))
			return;

		var tokenFiles = Directory.GetFiles(_rootFolder, $"*{tokenStorageKey}*", SearchOption.TopDirectoryOnly);

		foreach (var file in tokenFiles)
		{
			File.Delete(file);
		}
	}
}
