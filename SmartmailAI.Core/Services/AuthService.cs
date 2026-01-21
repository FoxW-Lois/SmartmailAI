using System.Threading.Tasks;
using SmartmailAI.Core.Contracts.Services;
using SmartmailAI.Core.Data;
using SmartmailAI.Core.IRepository;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Services;

public class AuthService(IAccountRepository accountRepository) : IAuthService
{
	private readonly IAccountRepository _accountRepository = accountRepository;

	public bool IsAuthenticated { get; private set; }
	public Account? CurrentAccount { get; private set; }

	// Connexion
	public async Task<(bool, string?)> LoginAsync(string login, string password)
	{
		var account = await _accountRepository.GetByLoginAsync(login);
		if (account is null)
			return (false, null);

		if (!account.Enabled)
			return (false, "Error_AccountDisabled");

		bool validPassword = Hasher.VerifyPassword(password, account.Password, account.Salt);

		if (!validPassword)
			return (false, null);

		IsAuthenticated = true;
		CurrentAccount = account;
		return (true, null);
	}

	// Inscription
	public async Task<(bool Success, string Error)> RegisterAsync(string login, string phoneNumber, string password)
	{
		if (await _accountRepository.LoginExistsAsync(login))
			return (false, "Ce login est déjà utilisé");

		if (password.Length < 8)
			return (false, "Mot de passe trop court");

		var (hash, salt) = Hasher.HashPassword(password);

		var account = new Account
		{
			Login = login,
			PhoneNumber = phoneNumber,
			Password = hash,
			Salt = salt,
			//TODO: mettre Enabled en false => désactivation par défaut des nouveaux comptes créés, activation à la main par l'admin
			Enabled = true
		};

		await _accountRepository.AddAsync(account);

		IsAuthenticated = true;
		CurrentAccount = account;
		return (true, string.Empty);
	}

	// Déconnexion
	public void Logout()
	{
		IsAuthenticated = false;
		CurrentAccount = null;
	}
}
