using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using SmartmailAI.Core.Contracts.Services.Addresses;

namespace SmartmailAI.Core.Services.Addresses;

public class OtherTokenStore : IOtherTokenStore
{
	private readonly string _rootFolder;

	public OtherTokenStore()
	{
		_rootFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Project", "MailTokens");

		Directory.CreateDirectory(_rootFolder);
	}

	public async Task SavePasswordAsync(string key, string password)
	{
		var path = GetPath(key);

		var encrypted = ProtectedData.Protect(Encoding.UTF8.GetBytes(password), null, DataProtectionScope.CurrentUser);

		await File.WriteAllBytesAsync(path, encrypted);
	}

	public async Task<string?> GetPasswordAsync(string key)
	{
		var path = GetPath(key);

		if (!File.Exists(path))
			return null;

		var encrypted = await File.ReadAllBytesAsync(path);

		var bytes = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);

		return Encoding.UTF8.GetString(bytes);
	}

	public void DeleteToken(string key)
	{
		var path = GetPath(key);

		if (File.Exists(path))
			File.Delete(path);
	}

	private string GetPath(string key)
	{
		return Path.Combine(_rootFolder, $"{key}.bin");
	}
}
