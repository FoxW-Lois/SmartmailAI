using System;
using System.Threading.Tasks;
using SmartmailAI.Core.Contracts.Services.Authentication;
using SmartmailAI.Core.Contracts.Repository;

namespace SmartmailAI.Core.Services.Authentication;

public class AccountSecretStore(IAccountRepository accountRepository) : IAccountSecretStore
{
	private readonly IAccountRepository _accountRepository = accountRepository;

	public async Task SaveSecretAsync(string login, string encryptedSecret)
	{
		var account = await _accountRepository.GetAccountByLoginAsync(login) ?? throw new InvalidOperationException("Utilisateur introuvable");

		account.EncryptedTotpSecret = encryptedSecret;
		account.TwoFactorEnabled = true;

		await _accountRepository.UpdateAccountAsync(account);
	}

	public async Task<string?> GetSecretAsync(string login)
	{
		var account = await _accountRepository.GetAccountByLoginAsync(login);
		return account?.EncryptedTotpSecret;
	}

	public async Task DeleteSecretAsync(string login)
	{
		var account = await _accountRepository.GetAccountByLoginAsync(login) ?? throw new InvalidOperationException("Utilisateur introuvable");

		account.EncryptedTotpSecret = null;
		account.TwoFactorEnabled = false;

		await _accountRepository.UpdateAccountAsync(account);
	}
}
