using System;
using System.Security.Cryptography;
using System.Text;
using SmartmailAI.Core.Contracts.Services.Authentication;

namespace SmartmailAI.Core.Services.Authentication;

public class CryptoService : ICryptoService
{
	public string Encrypt(string plaintext)
	{
		var data = Encoding.UTF8.GetBytes(plaintext);
		var protectedData = ProtectedData.Protect(data, optionalEntropy: null, DataProtectionScope.CurrentUser);

		return Convert.ToBase64String(protectedData);
	}

	public string Decrypt(string ciphertext)
	{
		var protectedData = Convert.FromBase64String(ciphertext);
		var data = ProtectedData.Unprotect(protectedData, optionalEntropy: null, DataProtectionScope.CurrentUser);

		return Encoding.UTF8.GetString(data);
	}
}
