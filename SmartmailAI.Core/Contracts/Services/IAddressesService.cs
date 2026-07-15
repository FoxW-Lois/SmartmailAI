using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Contracts.Services;

public interface IAddressesService
{
	bool HasAny { get; }

	event EventHandler<bool> AddressesListChanged;

	Task RefreshAddressesListAsync();

	Task<(bool success, AccountGmail? accountGmail, string? errorName)> AddGmailAccountAsync(string accountIndexGuid);

	Task<bool> AddOutlookAsync();

	Task<(bool success, AccountOther? accountOther, string? errorName)> AddOtherAddressAsync(AddOtherAddressRequest request, string accountIndexGuid);

	Task<bool> RemoveAddressAsync(AccountMailBase account);

	Task<AccountMailBase?> GetAccountByEmailAsync(string email);

	Task<List<AccountMailBase>> GetListAccountsLinkedAsync();
}
