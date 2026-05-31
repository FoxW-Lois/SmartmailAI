namespace SmartmailAI.Core.Contracts.Services.LocalSecurity;

public interface IDpapiService
{
	string Encrypt(string plaintext);

	string Decrypt(string ciphertext);
}
