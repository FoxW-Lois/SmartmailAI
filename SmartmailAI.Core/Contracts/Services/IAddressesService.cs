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

	Task<(bool, AccountGmail?, string?)> AddGmailAccountAsync();

	Task<bool> AddOutlookAsync();

	Task<bool> AddOtherAddressAsync();

	Task<bool> RemoveGmailAccountAsync(AccountGmail account);

	Task<List<AccountGmail>> GetListAccountsLinkedAsync();
}
