using System.Threading.Tasks;
using SmartmailAI.Core.Contracts.Repository;
using SmartmailAI.Core.Contracts.Services;
using SmartmailAI.Core.Contracts.Services.Authentication;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Services;

public class AccountService(ILocalSessionService localSessionService, IAccountRepository accountRepository) : IAccountService
{
	private readonly ILocalSessionService _localSessionService = localSessionService;
	private readonly IAccountRepository _accountRepository = accountRepository;

	public async Task<Account?> GetAccountByLoginInLocalSessionAsync()
	{
		// Récupère la session locale
		var localSession = _localSessionService.LoadSession();

		if (localSession is null)
			return null;

		string login = localSession.Login;
		var account = await _accountRepository.GetAccountByLoginAsync(login);

		return account;
	}
}
