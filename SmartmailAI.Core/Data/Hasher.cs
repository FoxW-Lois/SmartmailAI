using System;
using System.Security.Cryptography;

namespace SmartmailAI.Core.Data;

public class Hasher
{
	public static (string Hash, string Salt) HashPassword(string password)
	{
		var saltBytes = RandomNumberGenerator.GetBytes(16);
		var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 100_000, HashAlgorithmName.SHA256);
		var hashBytes = pbkdf2.GetBytes(32);

		return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
	}

	public static bool VerifyPassword(string password, string storedHash, string storedSalt)
	{
		var saltBytes = Convert.FromBase64String(storedSalt);
		var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 100_000, HashAlgorithmName.SHA256);
		var hashBytes = pbkdf2.GetBytes(32);

		return Convert.ToBase64String(hashBytes) == storedHash;
	}
}
