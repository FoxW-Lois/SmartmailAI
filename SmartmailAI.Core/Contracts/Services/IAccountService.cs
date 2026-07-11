using System.Threading.Tasks;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Contracts.Services;

public interface IAccountService
{
	Task<Account?> GetAccountByLoginInLocalSessionAsync();
}
