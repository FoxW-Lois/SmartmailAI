using System.Collections.Generic;
using System.Threading.Tasks;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Contracts.Repository;

public interface IAddressesRepository
{
	Task<List<AccountGmail>> GetAllAddressesAsync();

	Task<AccountGmail?> GetAddressByEmailAsync(string email);

	Task AddAddressAsync(AccountGmail accountGmail);

	Task<bool> DeleteAddressAsync(AccountGmail accountGmail);
}
