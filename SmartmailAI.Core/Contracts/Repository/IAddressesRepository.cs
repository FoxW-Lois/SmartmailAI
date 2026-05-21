using System.Collections.Generic;
using System.Threading.Tasks;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Contracts.Repository;

public interface IAddressesRepository
{
	Task<List<AccountMailBase>> GetAllAddressesAsync();

	Task<AccountMailBase?> GetAddressByEmailAsync(string email);

	Task AddAddressByGoogleAsync(AccountGmail accountGmail);

	Task AddAddressByOtherAsync(AccountOther accountOther);

	Task<bool> DeleteAddressByGoogleAsync(AccountGmail accountGmail);

	Task<bool> DeleteAddressByOtherAsync(AccountOther accountOther);
}
