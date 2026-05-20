using System.Collections.Generic;
using System.Threading.Tasks;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Contracts.Repository;

public interface IAddressesRepository
{
	Task<List<AccountMailBase>> GetAllAddressesAsync();

	Task<AccountMailBase?> GetAddressByEmailAsync(string email);

	Task AddAddressAsync(AccountGmail? accountGmail = null, AccountOther? accountOther = null);

	Task<bool> DeleteAddressAsync(AccountGmail? accountGmail = null, AccountOther? accountOther = null);
}
