namespace SmartmailAI.Core.Contracts.Services.LocalSecurity;

public interface ISecureDataService
{
	string Encrypt(string plaintext);

	string Decrypt(string ciphertext);
}
