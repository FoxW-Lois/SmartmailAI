using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartmailAI.Core.Contracts.Repository;
using SmartmailAI.Core.Contracts.Services;
using SmartmailAI.Core.Contracts.Services.Addresses;
using SmartmailAI.Core.Data;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Services;

public class AddressesService(IAccountService accountService, IAddressesRepository addressRepository, IEmailRepository emailRepository,
	IGmailCredentialService gmailCredentialService, IGmailApiService gmailApiService, IGmailLogoutService gmailLogoutService,
	IOtherCredentialService otherCredentialService, IOtherLogoutService otherLogoutService, IOtherTokenStore otherTokenStore) : IAddressesService
{
	private readonly IAccountService _accountService = accountService;
	private readonly IAddressesRepository _addressRepository = addressRepository;
	private readonly IEmailRepository _emailRepository = emailRepository;

	private readonly IGmailCredentialService _gmailCredentialService = gmailCredentialService;
	private readonly IGmailApiService _gmailApiService = gmailApiService;
	private readonly IGmailLogoutService _gmailLogoutService = gmailLogoutService;

	private readonly IOtherCredentialService _otherCredentialService = otherCredentialService;
	private readonly IOtherLogoutService _otherLogoutService = otherLogoutService;
	private readonly IOtherTokenStore _otherTokenStore = otherTokenStore;

	public bool HasAny { get; private set; }

	public event EventHandler<bool>? AddressesListChanged;

	public async Task RefreshAddressesListAsync()
	{
		var newValue = await _addressRepository.GetAllAddressesByAccountIndexGuidAsync();
		HasAny = newValue.Count > 0;
		AddressesListChanged?.Invoke(this, HasAny);
	}

	public async Task<(bool success, AccountGmail? accountGmail, string? errorName)> AddGmailAccountAsync(string accountIndexGuid)
	{
		var userKey = Guid.NewGuid().ToString();

		var credential = await _gmailCredentialService.ConnectAsync(userKey);
		if (credential is null)
			return (false, null, null); // null en 3ème position car déjà traité en cas par défaut par l'appelant

		var email = await _gmailApiService.GetEmailAddressAsync(credential);

		if (await CheckIfMailAccountExist(email))
			return (false, null, "EmailAccount_AlreadyExist");

		var account = new AccountGmail
		{
			IndexGuidHash = Hasher.HashDataWithoutSalt(accountIndexGuid),
			Email = email,
			GoogleUserId = credential.UserId,
			ConnectedAt = DateTime.UtcNow,
			IsFirstConnection = true,
			TokenStorageKey = userKey
		};

		await _addressRepository.AddAddressAsync(account);
		return (true, account, null);
	}

	public async Task<bool> AddOutlookAsync()
	{
		return true;
	}

	public async Task<(bool success, AccountOther? accountOther, string? errorName)> AddOtherAddressAsync(AddOtherAddressRequest request, string accountIndexGuid)
	{
		var userKey = Guid.NewGuid().ToString();

		if (await CheckIfMailAccountExist(request.Email))
			return (false, null, "EmailAccount_AlreadyExist");

		var account = new AccountOther
		{
			IndexGuidHash = Hasher.HashDataWithoutSalt(accountIndexGuid),
			Email = request.Email,
			UserName = request.UserName,
			Password = request.Password, // Mot de passe des adresses connectées par SMTP/IMAP jamais stocké
			ConnectedAt = DateTime.UtcNow,
			IsFirstConnection = true,
			TokenStorageKey = userKey,
			ImapHost = request.ImapHost,
			ImapPort = request.ImapPort,
			ImapUseSsl = request.ImapUseSsl,
			SmtpHost = request.SmtpHost,
			SmtpPort = request.SmtpPort,
			SmtpUseSsl = request.SmtpUseSsl
		};

		var success = await _otherCredentialService.ConnectAsync(account);
		if (!success)
			return (false, null, null); // null en 3ème position car déjà traité en cas par défaut par l'appelant

		// Stockage sécurisé du mot de passe en local
		await _otherTokenStore.SavePasswordAsync(account.TokenStorageKey, account.Password);

		// Supprime le mot de passe entré afin de ne surtout PAS le conserver en bdd
		account.Password = string.Empty;

		await _addressRepository.AddAddressAsync(account);
		return (true, account, null);
	}

	// Déconnexion
	public async Task<bool> RemoveAddressAsync(AccountMailBase account)
	{
		if (account is AccountGmail accountGmail)
			await _gmailLogoutService.LogoutAsync(accountGmail);
		else if (account is AccountOther accountOther)
			await _otherLogoutService.LogoutAsync(accountOther);
		// TODO: ajouter un check account is AccountOutlook accountOutlook

		await _emailRepository.DeleteAllEmailsAsync(account);
		await _addressRepository.DeleteAddressAsync(account);

		return true;
	}

	public async Task<AccountMailBase?> GetAccountByEmailAsync(string email)
	{
		var account = await _addressRepository.GetAddressByEmailAsync(email);
		return account ?? null;
	}

	public async Task<List<AccountMailBase>> GetListAccountsLinkedAsync()
	{
		var accounts = await _addressRepository.GetAllAddressesByAccountIndexGuidAsync();
		return accounts;
	}

	private async Task<bool> CheckIfMailAccountExist(string address)
	{
		var accountsList = await _addressRepository.GetAllAddressesByAccountIndexGuidAsync();

		foreach (var account in accountsList)
		{
			if (account.Email == address)
				return true;
		}

		return false;
	}
}
