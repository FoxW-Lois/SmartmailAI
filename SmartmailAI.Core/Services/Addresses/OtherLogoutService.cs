using System;
using System.IO;
using System.Threading.Tasks;
using SmartmailAI.Core.Contracts.Services.Addresses;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Services.Addresses;

public class OtherLogoutService(ITokenStore tokenStore) : IOtherLogoutService
{
	private readonly ITokenStore _tokenStore = tokenStore;

	private static readonly string _rootFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
		"SmartmailAI", "SMTP-IMAP.AuthToken");

	public Task LogoutAsync(AccountOther account)
	{
		_tokenStore.DeleteToken(account.TokenStorageKey, _rootFolder);

		return Task.CompletedTask;
	}
}
