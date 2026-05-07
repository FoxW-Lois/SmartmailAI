using System;
using System.IO;
using System.Text.Json;
using SmartmailAI.Core.Contracts.Services.Addresses;

namespace SmartmailAI.Core.Services.Addresses;

public class OutlookTokenStore : IOutlookTokenStore
{
	private static readonly string RootDir = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
		"SmartmailAI", "MSAL"
	);

	public void SaveAccountId(string tokenStorageKey, string homeAccountId)
	{
		Directory.CreateDirectory(RootDir);
		var path = GetFilePath(tokenStorageKey);

		File.WriteAllText(path, JsonSerializer.Serialize(new { HomeAccountId = homeAccountId }));
	}

	public string? GetAccountId(string tokenStorageKey)
	{
		var path = GetFilePath(tokenStorageKey);

		if (!File.Exists(path)) return null;

		var json = File.ReadAllText(path);
		var doc = JsonSerializer.Deserialize<JsonElement>(json);

		return doc.GetProperty("HomeAccountId").GetString();
	}

	public void DeleteToken(string tokenStorageKey)
	{
		var path = GetFilePath(tokenStorageKey);

		if (File.Exists(path))
			File.Delete(path);
	}

	private static string GetFilePath(string key) => Path.Combine(RootDir, $"{key}.json");
}
