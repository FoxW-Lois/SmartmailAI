using System.Collections.Generic;
using System.Threading.Tasks;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Contracts.Repository;

public interface IAddressesRepository
{
	Task<List<AccountMailBase>> GetAllAddressesAsync();

	Task<AccountMailBase?> GetAddressByEmailAsync(string email);

	Task AddAddressAsync(AccountMailBase account);

	Task UpdateAddressAsync(AccountMailBase account);

	Task DeleteAddressAsync(AccountMailBase account);

	Task<AccountMailBase> DecryptDataAsync(AccountMailBase account);
}
