using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmartmailAI.Core.Contracts.Services.Addresses;
using SmartmailAI.Core.Contracts.Services.Authentication;
using SmartmailAI.Core.Contracts.Repository;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Services.Addresses;

public class MailReaderService(IAddressesRepository repository, IGmailCredentialService credentialService, IGmailApiService gmailApi,
	IAuthService authService, IAccountRepository accountRepository) : IMailReaderService
{
	private readonly IAddressesRepository _addressesRepository = repository;
	private readonly IGmailCredentialService _gmailCredentialService = credentialService;
	private readonly IGmailApiService _gmailApiService = gmailApi;
	private readonly IAuthService _authService = authService;
	private readonly IAccountRepository _accountRepository = accountRepository;

	public async Task<IReadOnlyList<EmailGmail>> GetLastMessagesFromAccountAsync(AccountGmail accountGmail, bool isAddingNewAddress)
	{
		var credential = await _gmailCredentialService.GetCredentialAsync(accountGmail);
		if (credential == null)
			return [];

		string currentAccountLogin = _authService.CurrentAccountLogin;
		var currentAccount = await _accountRepository.GetByLoginAsync(currentAccountLogin);

		DateTime? lastConnection;
		List<EmailGmail> emailsList;
		int numMails = 10;

		if (currentAccount != null)
		{
			lastConnection = currentAccount.LastConnection;
			emailsList = await _gmailApiService.GetLastMessagesAsync(credential, "Inbox", isAddingNewAddress, numMails, lastConnection);
			emailsList.AddRange(await _gmailApiService.GetLastMessagesAsync(credential, "Sent", isAddingNewAddress, numMails, lastConnection));
		}
		else
		{
			emailsList = await _gmailApiService.GetLastMessagesAsync(credential, "Inbox", isAddingNewAddress, numMails);
			emailsList.AddRange(await _gmailApiService.GetLastMessagesAsync(credential, "Sent", isAddingNewAddress, numMails));
		}

		return emailsList;
	}
}
