using System.Threading.Tasks;
using SmartmailAI.Core.Contracts.Services.Addresses;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Services.Addresses;

public class OtherLogoutService(ITokenStore tokenStore) : IOtherLogoutService
{
	private readonly ITokenStore _tokenStore = tokenStore;

	public Task LogoutAsync(AccountOther account)
	{
		_tokenStore.DeleteToken(account.TokenStorageKey);

		return Task.CompletedTask;
	}
}
