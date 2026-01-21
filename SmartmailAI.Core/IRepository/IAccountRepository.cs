using System.Threading.Tasks;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.IRepository;

public interface IAccountRepository
{
	Task<Account?> GetByLoginAsync(string login);

	Task<bool> LoginExistsAsync(string login);

	Task AddAsync(Account account);
}
