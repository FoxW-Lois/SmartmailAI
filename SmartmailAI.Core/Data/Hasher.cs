using System;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace SmartmailAI.Core.Data;

public class Hasher
{
	private const int SaltSize = 16; // 16 octets => 128 bits
	private const int HashSize = 32; // 32 octets => 256 bits

	// Paramètres Argon2id
	private const int DegreeOfParallelism = 3; // Nombre de threads
	private const int Iterations = 4; // Nombre d'itérations
	private const int MemorySize = 1024 * 128; // 128 MB

	public static (string Hash, string Salt) HashPassword(string password)
	{
		byte[] saltBytes = RandomNumberGenerator.GetBytes(SaltSize);

		// Configuration Argon2id
		var argon2 = new Argon2id(System.Text.Encoding.UTF8.GetBytes(password))
		{
			Salt = saltBytes,
			DegreeOfParallelism = DegreeOfParallelism,
			Iterations = Iterations,
			MemorySize = MemorySize
		};

		// Calcul du hash
		byte[] hashBytes = argon2.GetBytes(HashSize);

		return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
	}

	public static bool VerifyPassword(string password, string storedHash, string storedSalt)
	{
		byte[] saltBytes = Convert.FromBase64String(storedSalt);

		// Configuration Argon2id
		var argon2 = new Argon2id(System.Text.Encoding.UTF8.GetBytes(password))
		{
			Salt = saltBytes,
			DegreeOfParallelism = DegreeOfParallelism,
			Iterations = Iterations,
			MemorySize = MemorySize
		};

		// Calcul du hash
		byte[] computedHash = argon2.GetBytes(HashSize);

		// Comparaison sécurisée contre les timing attacks
		return CryptographicOperations.FixedTimeEquals(computedHash, Convert.FromBase64String(storedHash));
	}

	public static string HashDataWithoutSalt(string data)
	{
		var bytes = Encoding.UTF8.GetBytes(data);
		var hash = SHA256.HashData(bytes);

		return Convert.ToBase64String(hash);
	}
}
