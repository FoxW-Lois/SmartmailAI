using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartmailAI.Core.Contracts.Repository;
using SmartmailAI.Core.Contracts.Services;
using SmartmailAI.Core.Contracts.Services.Addresses;
using SmartmailAI.Core.Contracts.Services.Authentication;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Services.Addresses;

public class MailReaderService(IGmailCredentialService credentialService, IGmailApiService gmailApi, IAuthService authService, 
	IAccountRepository accountRepository, IMappersToEmailDTOService mappersToEmailDTOService) : IMailReaderService
{
	private readonly IGmailCredentialService _gmailCredentialService = credentialService;
	private readonly IGmailApiService _gmailApiService = gmailApi;
	private readonly IAuthService _authService = authService;
	private readonly IAccountRepository _accountRepository = accountRepository;
	private readonly IMappersToEmailDTOService _mappersToEmailDTOService = mappersToEmailDTOService;

	public async Task<IReadOnlyList<Email>> GetLastMessagesFromAccountAsync(AccountGmail accountGmail, bool isAddingNewAddress)
	{
		var credential = await _gmailCredentialService.GetCredentialAsync(accountGmail);
		if (credential == null)
			return [];

		string currentAccountLogin = _authService.CurrentAccountLogin;
		var currentAccount = await _accountRepository.GetAccountByLoginAsync(currentAccountLogin);

		DateTime? lastConnection;
		List<EmailGmail> rawEmailsList;
		int numMails = 10;

		if (currentAccount != null)
		{
			lastConnection = currentAccount.LastConnection;
			rawEmailsList = await _gmailApiService.GetLastMessagesAsync(credential, "Inbox", isAddingNewAddress, numMails, lastConnection);
			rawEmailsList.AddRange(await _gmailApiService.GetLastMessagesAsync(credential, "Sent", isAddingNewAddress, numMails, lastConnection));
		}
		else
		{
			rawEmailsList = await _gmailApiService.GetLastMessagesAsync(credential, "Inbox", isAddingNewAddress, numMails);
			rawEmailsList.AddRange(await _gmailApiService.GetLastMessagesAsync(credential, "Sent", isAddingNewAddress, numMails));
		}

		List<Email> emailsList = await _mappersToEmailDTOService.MapEmailGmailToEmail_List(rawEmailsList);

		return emailsList;
	}
}
