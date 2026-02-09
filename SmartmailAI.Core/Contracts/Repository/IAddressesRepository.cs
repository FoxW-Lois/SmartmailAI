using System.Collections.Generic;
using System.Threading.Tasks;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Contracts.Repository;

public interface IAddressesRepository
{
	Task<List<AccountGmail>> GetAllAddressAsync();

	Task<AccountGmail?> GetByEmailAsync(string email);

	Task AddAsync(AccountGmail accountGmail);

	Task<bool> DeleteAsync(AccountGmail accountGmail);
}
