using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartmailAI.Core.Contracts.Repository;
using SmartmailAI.Core.Contracts.Services;
using SmartmailAI.Core.Contracts.Services.Addresses;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Services;

public class AddressesService(IAddressesRepository addressRepository, IGmailCredentialService gmailCredentialService, IGmailApiService gmailApiService,
	IGmailLogoutService gmailLogoutService, IEmailRepository emailRepository) : IAddressesService
{
	private readonly IAddressesRepository _addressRepository = addressRepository;
	private readonly IGmailCredentialService _gmailCredentialService = gmailCredentialService;
	private readonly IGmailApiService _gmailApiService = gmailApiService;
	private readonly IGmailLogoutService _gmailLogoutService = gmailLogoutService;
	private readonly IEmailRepository _emailRepository = emailRepository;

	public bool HasAny { get; private set; }

	public event EventHandler<bool>? AddressesListChanged;

	public async Task RefreshAddressesListAsync()
	{
		var newValue = await _addressRepository.GetAllAddressAsync();
		HasAny = newValue.Count > 0;
		AddressesListChanged?.Invoke(this, HasAny);
	}

	public async Task<(bool, string?)> AddGmailAccountAsync()
	{
		var userKey = Guid.NewGuid().ToString();

		var credential = await _gmailCredentialService.ConnectAsync(userKey);
		var email = await _gmailApiService.GetEmailAddressAsync(credential);

		if (await CheckIfGmailAccountExist(email))
			return (false, "Email_AlreadyExist");

		var account = new AccountGmail
		{
			Email = email,
			GoogleUserId = credential.UserId,
			ConnectedAt = DateTime.UtcNow,
			TokenStorageKey = userKey
		};

		await _addressRepository.AddAsync(account);
		return (true, null);
	}

	public async Task<bool> AddOutlookAsync()
	{
		return true;
	}

	public async Task<bool> AddOtherAddressAsync()
	{
		return true;
	}

	// Déconnexion
	public async Task<bool> RemoveGmailAccountAsync(AccountGmail account)
	{
		await _gmailLogoutService.LogoutAsync(account);
		await _emailRepository.DeleteAllEmailsAsync(account);
		await _addressRepository.DeleteAsync(account);

		return true;
	}

	public async Task<List<AccountGmail>> GetListAccountsLinkedAsync()
	{
		var accounts = await _addressRepository.GetAllAddressAsync();
		return accounts;
	}

	public async Task<bool> CheckIfGmailAccountExist(string addresseGmail)
	{
		var accountsGmailList = await _addressRepository.GetAllAddressAsync();

		foreach (var account in accountsGmailList)
		{
			if (account.Email == addresseGmail)
				return true;
		}

		return false;
	}
}
