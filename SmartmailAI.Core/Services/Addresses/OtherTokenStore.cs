using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using SmartmailAI.Core.Contracts.Services.Addresses;

namespace SmartmailAI.Core.Services.Addresses;

public class OtherTokenStore : IOtherTokenStore
{
	private static readonly string _rootFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
		"SmartmailAI", "SMTP-IMAP.AuthToken");

	public OtherTokenStore()
	{
		Directory.CreateDirectory(_rootFolder);
	}

	public async Task SavePasswordAsync(string key, string password)
	{
		var path = GetPath(key);

		byte[] data = Encoding.UTF8.GetBytes(password);

		byte[] encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);

		await File.WriteAllBytesAsync(path, encrypted);
	}

	public async Task<string?> GetPasswordAsync(string key)
	{
		var path = GetPath(key);

		if (!File.Exists(path))
			return null;

		byte[] encrypted = await File.ReadAllBytesAsync(path);

		byte[] decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);

		return Encoding.UTF8.GetString(decrypted);
	}

	// La suppression de token est gérée par la suppression de l'adresse correspondante (via OtherLogoutService), donc pas besoin d'une méthode dédiée ici

	private static string GetPath(string key)
	{
		return Path.Combine(_rootFolder, $"{key}.bin");
	}
}
