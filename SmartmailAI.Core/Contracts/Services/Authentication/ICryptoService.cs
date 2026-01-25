namespace SmartmailAI.Core.Contracts.Services.Authentication;

public interface ICryptoService
{
	string Encrypt(string plaintext);

	string Decrypt(string ciphertext);
}
