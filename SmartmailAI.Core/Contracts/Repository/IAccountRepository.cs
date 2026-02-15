using System.Threading.Tasks;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Contracts.Repository;

public interface IAccountRepository
{
	Task<Account?> GetAccountByLoginAsync(string login);

	Task<bool> LoginExistsAsync(string login);

	Task AddAccountAsync(Account account);

	Task UpdateAccountAsync(Account account);

	Task DeleteAccountAsync(Account account);
}
