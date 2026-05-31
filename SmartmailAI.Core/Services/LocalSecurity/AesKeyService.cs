using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using SmartmailAI.Core.Contracts.Services.LocalSecurity;
using Windows.Storage;

namespace SmartmailAI.Core.Services.LocalSecurity;

public class AesKeyService(IDpapiService dpapi) : IAesKeyService
{
	private readonly IDpapiService _dpapi = dpapi;

	private const string KeyFileName = "aes.key";

	public async Task<byte[]> GetOrCreateKeyAsync()
	{
		var localFolder = ApplicationData.Current.LocalFolder;
		var keyPath = Path.Combine(localFolder.Path, KeyFileName);

		if (File.Exists(keyPath))
		{
			var encryptedKey = await File.ReadAllTextAsync(keyPath);
			var decrypted = _dpapi.Decrypt(encryptedKey);
			return Convert.FromBase64String(decrypted);
		}

		// Création clé AES 256
		using var aes = Aes.Create();
		aes.KeySize = 256;
		aes.GenerateKey();

		var keyBytes = aes.Key;

		var base64 = Convert.ToBase64String(keyBytes);
		var encrypted = _dpapi.Encrypt(base64);

		await File.WriteAllTextAsync(keyPath, encrypted);

		return keyBytes;
	}
}
