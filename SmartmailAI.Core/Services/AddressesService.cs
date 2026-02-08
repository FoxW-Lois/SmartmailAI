using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartmailAI.Core.Contracts.Services;
using SmartmailAI.Core.Contracts.Services.Addresses;
using SmartmailAI.Core.IRepository;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Services;

public class AddressesService(IAddressesRepository addressRepository, IGmailCredentialService gmailCredentialService, IGmailApiService gmailApiService,
	IGmailLogoutService gmailLogoutService) : IAddressesService
{
	private readonly IAddressesRepository _addressRepository = addressRepository;
	private readonly IGmailCredentialService _gmailCredentialService = gmailCredentialService;
	private readonly IGmailApiService _gmailApiService = gmailApiService;
	private readonly IGmailLogoutService _gmailLogoutService = gmailLogoutService;

	public bool HasAny { get; private set; }

	public event EventHandler<bool>? AddressesListChanged;

	public async Task RefreshAddressesListAsync()
	{
		var newValue = await _addressRepository.GetAllAddressAsync();
		HasAny = newValue.Count > 0;
		AddressesListChanged?.Invoke(this, HasAny);
	}

	public async Task<(AccountGmail, bool)> AddGmailAccountAsync()
	{
		var userKey = Guid.NewGuid().ToString();

		var credential = await _gmailCredentialService.ConnectAsync(userKey);
		var email = await _gmailApiService.GetEmailAddressAsync(credential);

		var account = new AccountGmail
		{
			Email = email,
			GoogleUserId = credential.UserId,
			ConnectedAt = DateTime.UtcNow,
			TokenStorageKey = userKey
		};

		await _addressRepository.AddAsync(account);
		return (account, true);
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
		await _addressRepository.DeleteAsync(account);

		return true;
	}

	public async Task<List<AccountGmail>> GetListAccountsLinkedAsync()
	{
		var accounts = await _addressRepository.GetAllAddressAsync();
		return accounts;
	}
}
