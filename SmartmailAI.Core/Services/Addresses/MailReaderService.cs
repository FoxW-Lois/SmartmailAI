using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartmailAI.Core.Contracts.Repository;
using SmartmailAI.Core.Contracts.Services;
using SmartmailAI.Core.Contracts.Services.Addresses;
using SmartmailAI.Core.Contracts.Services.Authentication;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Services.Addresses;

public class MailReaderService(IEmailRepository emailRepository, IGmailCredentialService gmailCredentialService, IGmailApiService gmailApiService,
	IOtherCredentialService otherCredentialService, IOtherProtocolService otherProtocolService, IOtherTokenStore otherTokenStore,
	IAuthService authService, IAccountRepository accountRepository, IMappersToEmailDTOService mappersToEmailDTOService) : IMailReaderService
{
	private readonly IEmailRepository _emailRepository = emailRepository;
	private readonly IGmailCredentialService _gmailCredentialService = gmailCredentialService;
	private readonly IGmailApiService _gmailApiService = gmailApiService;

	private readonly IOtherCredentialService _otherCredentialService = otherCredentialService;
	private readonly IOtherProtocolService _otherProtocolService = otherProtocolService;
	private readonly IOtherTokenStore _otherTokenStore = otherTokenStore;

	private readonly IAuthService _authService = authService;
	private readonly IAccountRepository _accountRepository = accountRepository;
	private readonly IMappersToEmailDTOService _mappersToEmailDTOService = mappersToEmailDTOService;

	public async Task<IReadOnlyList<Email>> GetLastMessagesFromAccountAsync(bool isAddingNewAddress, AccountMailBase account)
	{
		const int NumMails = 2;

		var lastConnection = await GetCurrentAccountLastConnectionAsync();

		List<EmailFromAddress> rawEmails;

		if (account is AccountGmail accountGmail)
			rawEmails = await GetGmailMessagesAsync(accountGmail, isAddingNewAddress, NumMails, lastConnection);
		else if (account is AccountOther accountOther)
			rawEmails = await GetOtherMessagesAsync(accountOther, isAddingNewAddress, NumMails, lastConnection);
		else
			return [];
		// TODO: ajouter un check account is AccountOutlook accountOutlook

		List<Email> emailsRecovered = await _mappersToEmailDTOService.MapEmailFromAddressToEmail_List(rawEmails);

		if (isAddingNewAddress)
			return emailsRecovered;

		return await _emailRepository.KeepOnlyNewEmailsAsync(account.Email, emailsRecovered);
	}

	#region Getting emails helpers

	private async Task<List<EmailFromAddress>> GetGmailMessagesAsync(AccountGmail account, bool isAddingNewAddress, int numMails, DateTime? lastConnection)
	{
		var credential = await _gmailCredentialService.GetCredentialAsync(account);

		if (credential is null)
			return [];

		var inboxTask = _gmailApiService.GetLastMessagesAsync(credential, "Inbox", isAddingNewAddress, numMails,
			lastConnection);

		var sentTask = _gmailApiService.GetLastMessagesAsync(credential, "Sent", isAddingNewAddress, numMails,
			lastConnection);

		await Task.WhenAll(inboxTask, sentTask);

		return [.. await inboxTask, .. await sentTask];
	}

	private async Task<List<EmailFromAddress>> GetOtherMessagesAsync(AccountOther account, bool isAddingNewAddress, int numMails, DateTime? lastConnection)
	{
		var connected = await PrepareOtherAccountAsync(account);

		if (!connected)
			return [];

		var inboxTask = _otherProtocolService.GetLastMessagesAsync(account, "Inbox", isAddingNewAddress, numMails,
			lastConnection);

		var sentTask = _otherProtocolService.GetLastMessagesAsync(account, "Sent", isAddingNewAddress, numMails,
			lastConnection);

		await Task.WhenAll(inboxTask, sentTask);

		return [.. await inboxTask, .. await sentTask];
	}

	#endregion Getting emails helpers

	public async Task SaveAttachmentFromEmailAsync(string messageId, MailAttachment attachment, string destinationFolder, AccountMailBase account)
	{
		if (account is AccountGmail accountGmail)
		{
			var credential = await _gmailCredentialService.GetCredentialAsync(accountGmail);

			if (credential is null)
				return;

			await _gmailApiService.SaveAttachmentAsync(credential, messageId, attachment, destinationFolder);

			return;
		}

		if (account is AccountOther accountOther)
		{
			var connected = await PrepareOtherAccountAsync(accountOther);

			if (!connected)
				return;

			await _otherProtocolService.SaveAttachmentAsync(accountOther, messageId, attachment, destinationFolder);
		}
	}

	#region Other account helpers

	private async Task<bool> PrepareOtherAccountAsync(AccountOther account)
	{
		string? password = await _otherTokenStore.GetPasswordAsync(account.TokenStorageKey);

		if (password is null)
			return false;

		account.Password = password;

		return await _otherCredentialService.ConnectAsync(account);
	}

	private async Task<DateTime?> GetCurrentAccountLastConnectionAsync()
	{
		string currentAccountLogin = _authService.CurrentAccountLogin;

		var currentAccount = await _accountRepository.GetAccountByLoginAsync(currentAccountLogin);

		return currentAccount?.LastConnection;
	}

	#endregion Other account helpers
}
