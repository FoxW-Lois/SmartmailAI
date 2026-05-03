using System;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SmartmailAI.Core.Contracts.Services.Authentication;
using SmartmailAI.Core.Data;
using SmartmailAI.Core.Contracts.Repository;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Services.Authentication;

public class AuthService(IAccountRepository accountRepository, IAccountSecretStore secretStore, ITotpService totpService,
	ICryptoService cryptoService) : IAuthService
{
	private readonly IAccountRepository _accountRepository = accountRepository;
	private readonly IAccountSecretStore _secretStore = secretStore;
	private readonly ITotpService _totpService = totpService;
	private readonly ICryptoService _cryptoService = cryptoService;

	#region Notification du changement d'état concernant l'authentification de l'utilisateur

	public event EventHandler<bool>? AuthenticationStateChanged;

	private bool _isAuthenticated = false;

	public bool IsAuthenticated
	{
		get => _isAuthenticated;
		private set
		{
			if (_isAuthenticated != value)
			{
				_isAuthenticated = value;
				AuthenticationStateChanged?.Invoke(this, value);
			}
		}
	}

	#endregion Notification du changement d'état concernant l'authentification de l'utilisateur

	// Exposition du login de l'instance en cours
	public string CurrentAccountLogin { get; private set; } = "";

	public async Task<bool> TryRestoreSessionAsync()
	{
		// TODO: à faire avec cookie de session (token stocké localement ?)
		IsAuthenticated = false;
		return IsAuthenticated;
	}

	// Connexion
	public async Task<(bool Success, string? SpecificError)> LoginAsync(string login, string password)
	{
		var account = await _accountRepository.GetAccountByLoginAsync(login);
		if (account is null)
			return (false, null);

		if (!account.Enabled)
			return (false, "Error_AccountDisabled");

		bool validPassword = Hasher.VerifyPassword(password, account.Password, account.Salt);

		if (!validPassword)
			return (false, null);

		if (account.TwoFactorEnabled)
			return (false, "Need_TwoFactor");

		IsAuthenticated = true;
		CurrentAccountLogin = login;
		return (true, null);
	}

	// Inscription
	public async Task<(bool Success, string Error)> RegisterAsync(string login, string phoneNumber, string password)
	{
		if (await _accountRepository.LoginExistsAsync(login))
			return (false, "Ce login est déjà utilisé.");

		if (password.Length < 12)
			return (false, "Mot de passe trop court.");

		if (password.Length < 12 || !Regex.IsMatch(password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).+$"))
			return (false, "Mot de passe pas assez fort.");

		if (!Regex.IsMatch(phoneNumber, @"^\d{10}$"))
			return (false, "Numéro de téléphone invalide.");

		var (hash, salt) = Hasher.HashPassword(password);

		var account = new Account
		{
			Login = login,
			PhoneNumber = phoneNumber,
			Password = hash,
			Salt = salt,
			TwoFactorEnabled = false,
			//TODO: mettre Enabled en false => désactivation par défaut des nouveaux comptes créés, activation à la main par l'admin
			Enabled = true,
			LastConnection = DateTime.Now
		};

		await _accountRepository.AddAccountAsync(account);

		return (true, string.Empty);
	}

	// Déconnexion
	public void Logout()
	{
		IsAuthenticated = false;
	}

	public async Task UpdateLastConnection()
	{
		string currentAccountLogin = CurrentAccountLogin;
		var currentAccount = await _accountRepository.GetAccountByLoginAsync(currentAccountLogin);
		if (currentAccount == null) return;

		currentAccount = new Account
		{
			Id = currentAccount.Id,
			Login = currentAccount.Login,
			PhoneNumber = currentAccount.PhoneNumber,
			Password = currentAccount.Password,
			Salt = currentAccount.Salt,
			TwoFactorEnabled = currentAccount.TwoFactorEnabled,
			Enabled = currentAccount.Enabled,
			LastConnection = DateTime.Now
		};

		await _accountRepository.UpdateAccountAsync(currentAccount);
	}

	public async Task Update_EnableDisable_TwoFactorAsync(string login, bool setEnable)
	{
		var user = await _accountRepository.GetAccountByLoginAsync(login) ?? throw new InvalidOperationException("Utilisateur introuvable");

		if (setEnable) // Activation
		{
			// Génération de la clé secrète
			var secret = _totpService.GenerateSecret();
			// Chiffrement de la clé
			var encryptedSecret = _cryptoService.Encrypt(secret.Base32);
			// Stockage de la clé
			await _secretStore.SaveSecretAsync(login, encryptedSecret);

			user.TwoFactorEnabled = true;
		}
		else // Désactivation
		{
			await _secretStore.DeleteSecretAsync(login);

			user.TwoFactorEnabled = false;
		}
	}

	public async Task<bool> ValidateSecondFactorAsync(string login, string code)
	{
		var user = await _accountRepository.GetAccountByLoginAsync(login);

		if (user is null || !user.TwoFactorEnabled)
			return false;

		var encryptedSecret = await _secretStore.GetSecretAsync(login);

		if (string.IsNullOrWhiteSpace(encryptedSecret))
			return false;

		// Déchiffrement de la clé
		var secret = _cryptoService.Decrypt(encryptedSecret);

		// Validation TOTP
		bool codeValidation = _totpService.ValidateCode(secret, code);

		if (codeValidation)
			IsAuthenticated = true;
		else
			IsAuthenticated = false;

		return codeValidation;
	}
}
