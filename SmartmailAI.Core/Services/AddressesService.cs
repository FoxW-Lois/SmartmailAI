using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartmailAI.Core.Contracts.Repository;
using SmartmailAI.Core.Contracts.Services;
using SmartmailAI.Core.Contracts.Services.Addresses;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Services;

public class AddressesService(IAddressesRepository addressRepository, IEmailRepository emailRepository,
	IGmailCredentialService gmailCredentialService, IGmailApiService gmailApiService, IGmailLogoutService gmailLogoutService,
	IOtherCredentialService otherCredentialService, IOtherProtocolService otherProtocolService, IOtherLogoutService otherLogoutService,
	IOtherTokenStore otherTokenStore) : IAddressesService
{
	private readonly IAddressesRepository _addressRepository = addressRepository;
	private readonly IEmailRepository _emailRepository = emailRepository;

	private readonly IGmailCredentialService _gmailCredentialService = gmailCredentialService;
	private readonly IGmailApiService _gmailApiService = gmailApiService;
	private readonly IGmailLogoutService _gmailLogoutService = gmailLogoutService;

	private readonly IOtherCredentialService _otherCredentialService = otherCredentialService;
	private readonly IOtherProtocolService _otherProtocolService = otherProtocolService;
	private readonly IOtherLogoutService _otherLogoutService = otherLogoutService;
	private readonly IOtherTokenStore _otherTokenStore = otherTokenStore;

	public bool HasAny { get; private set; }

	public event EventHandler<bool>? AddressesListChanged;

	public async Task RefreshAddressesListAsync()
	{
		var newValue = await _addressRepository.GetAllAddressesAsync();
		HasAny = newValue.Count > 0;
		AddressesListChanged?.Invoke(this, HasAny);
	}

	public async Task<(bool, AccountGmail?, string?)> AddGmailAccountAsync()
	{
		var userKey = Guid.NewGuid().ToString();

		var credential = await _gmailCredentialService.ConnectAsync(userKey);
		if (credential == null)
			return (false, null, null); // null en 3ème position car déjà traité en cas par défaut par l'appelant

		var email = await _gmailApiService.GetEmailAddressAsync(credential);

		if (await CheckIfMailAccountExist(email))
			return (false, null, "EmailAccount_AlreadyExist");

		var account = new AccountGmail
		{
			Email = email,
			GoogleUserId = credential.UserId,
			ConnectedAt = DateTime.UtcNow,
			TokenStorageKey = userKey
		};

		await _addressRepository.AddAddressAsync(account);
		return (true, account, null);
	}

	public async Task<bool> AddOutlookAsync()
	{
		return true;
	}

	public async Task<(bool, AccountOther?, string?)> AddOtherAddressAsync(AddOtherAddressRequest request)
	{
		var userKey = Guid.NewGuid().ToString();

		if (await CheckIfMailAccountExist(request.Email))
			return (false, null, "EmailAccount_AlreadyExist");

		var account = new AccountOther
		{
			Email = request.Email,
			UserName = request.UserName,
			Password = request.Password,
			ConnectedAt = DateTime.UtcNow,
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
		{
			// TODO: Voir supprimer les fichiers de OAuth locaux => Users\<user>\AppData\Roaming\Google.Apis.Auth
			await _gmailLogoutService.LogoutAsync(accountGmail);
		}
		else if (account is AccountOther accountOther)
		{
			_otherTokenStore.DeleteToken(accountOther.TokenStorageKey);
			await _otherLogoutService.LogoutAsync(accountOther);
		}
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
		var accounts = await _addressRepository.GetAllAddressesAsync();
		return accounts;
	}

	private async Task<bool> CheckIfMailAccountExist(string address)
	{
		var accountsList = await _addressRepository.GetAllAddressesAsync();

		foreach (var account in accountsList)
		{
			if (account.Email == address)
				return true;
		}

		return false;
	}
}
