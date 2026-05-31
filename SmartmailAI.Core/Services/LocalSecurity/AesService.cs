using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using SmartmailAI.Core.Contracts.Services.LocalSecurity;

namespace SmartmailAI.Core.Services.LocalSecurity;

public class AesService(IAesKeyService aesKeyService) : IAesService
{
	private readonly IAesKeyService _aesKeyService = aesKeyService;
	private byte[]? _key;

	private async Task<byte[]> GetKeyAsync()
	{
		_key ??= await _aesKeyService.GetOrCreateKeyAsync();
		return _key;
	}

	public async Task<string> EncryptAsync(string plainText)
	{
		var key = await GetKeyAsync();

		using var aes = Aes.Create();
		aes.Key = key;
		aes.GenerateIV();

		using var encryptor = aes.CreateEncryptor();

		var plainBytes = Encoding.UTF8.GetBytes(plainText);
		var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

		var result = new byte[aes.IV.Length + cipherBytes.Length];
		Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
		Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

		return Convert.ToBase64String(result);
	}

	public async Task<string> DecryptAsync(string cipherText)
	{
		var key = await GetKeyAsync();

		var full = Convert.FromBase64String(cipherText);

		using var aes = Aes.Create();
		aes.Key = key;

		var ivSize = aes.BlockSize / 8;
		var iv = full.Take(ivSize).ToArray();
		var data = full.Skip(ivSize).ToArray();

		aes.IV = iv;

		using var decryptor = aes.CreateDecryptor();
		var decrypted = decryptor.TransformFinalBlock(data, 0, data.Length);

		return Encoding.UTF8.GetString(decrypted);
	}
}
