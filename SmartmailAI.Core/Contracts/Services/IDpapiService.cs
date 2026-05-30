namespace SmartmailAI.Core.Contracts.Services;

public interface IDpapiService
{
	string Encrypt(string plaintext);

	string Decrypt(string ciphertext);
}
