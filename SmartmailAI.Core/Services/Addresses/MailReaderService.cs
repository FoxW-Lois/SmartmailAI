using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartmailAI.Core.Contracts.Repository;
using SmartmailAI.Core.Contracts.Services;
using SmartmailAI.Core.Contracts.Services.Addresses;
using SmartmailAI.Core.Contracts.Services.Authentication;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Services.Addresses;

public class MailReaderService(IGmailCredentialService gmailCredentialService, IGmailApiService gmailApiService,
	IOtherCredentialService otherCredentialService, IOtherProtocolService otherProtocolService, IOtherTokenStore otherTokenStore,
	IAuthService authService, IAccountRepository accountRepository, IMappersToEmailDTOService mappersToEmailDTOService) : IMailReaderService
{
	private readonly IGmailCredentialService _gmailCredentialService = gmailCredentialService;
	private readonly IGmailApiService _gmailApiService = gmailApiService;

	private readonly IOtherCredentialService _otherCredentialService = otherCredentialService;
	private readonly IOtherProtocolService _otherProtocolService = otherProtocolService;
	private readonly IOtherTokenStore _otherTokenStore = otherTokenStore;

	private readonly IAuthService _authService = authService;
	private readonly IAccountRepository _accountRepository = accountRepository;
	private readonly IMappersToEmailDTOService _mappersToEmailDTOService = mappersToEmailDTOService;

	public async Task<IReadOnlyList<Email>> GetLastMessagesFromAccountAsync(bool isAddingNewAddress, AccountGmail? accountGmail = null,
		AccountOther? accountOther = null)
	{
		List<Email> emailsList = [];
		DateTime? lastConnection;
		List<EmailFromAddress> rawEmailsList;
		int numMails = 2;

		if (accountGmail != null)
		{
			var credential = await _gmailCredentialService.GetCredentialAsync(accountGmail);
			if (credential == null)
				return [];

			string currentAccountLogin = _authService.CurrentAccountLogin;
			var currentAccount = await _accountRepository.GetAccountByLoginAsync(currentAccountLogin);

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

			emailsList = await _mappersToEmailDTOService.MapEmailFromAddressToEmail_List(rawEmailsList);
		}
		else if (accountOther != null)
		{
			string? password = await _otherTokenStore.GetPasswordAsync(accountOther.TokenStorageKey);
			if (password == null) return [];

			// On récupère le mot de passe stocké pour le compte et on le set dans l'accountOther pour pouvoir se connecter via IMAP/SMTP
			accountOther.Password = password;

			var success = await _otherCredentialService.ConnectAsync(accountOther);
			if (!success)
				return [];

			string currentAccountLogin = _authService.CurrentAccountLogin;
			var currentAccount = await _accountRepository.GetAccountByLoginAsync(currentAccountLogin);

			if (currentAccount != null)
			{
				lastConnection = currentAccount.LastConnection;
				rawEmailsList = await _otherProtocolService.GetLastMessagesAsync(accountOther, "Inbox", isAddingNewAddress, numMails, lastConnection);
				rawEmailsList.AddRange(await _otherProtocolService.GetLastMessagesAsync(accountOther, "Sent", isAddingNewAddress, numMails, lastConnection));
			}
			else
			{
				rawEmailsList = await _otherProtocolService.GetLastMessagesAsync(accountOther, "Inbox", isAddingNewAddress, numMails);
				rawEmailsList.AddRange(await _otherProtocolService.GetLastMessagesAsync(accountOther, "Sent", isAddingNewAddress, numMails));
			}

			emailsList = await _mappersToEmailDTOService.MapEmailFromAddressToEmail_List(rawEmailsList);
		}

		return emailsList;
	}

	public async Task SaveAttachmentFromEmailAsync(string messageId, MailAttachment attachment, string destinationFolder,
		AccountGmail? accountGmail = null, AccountOther? accountOther = null)
	{
		if (accountGmail != null)
		{
			var credential = await _gmailCredentialService.GetCredentialAsync(accountGmail);
			if (credential == null)
				return;

			await _gmailApiService.SaveAttachmentAsync(credential, messageId, attachment, destinationFolder);
		}

		if (accountOther != null)
		{
			string? password = await _otherTokenStore.GetPasswordAsync(accountOther.TokenStorageKey);
			if (password == null) return;

			// On récupère le mot de passe stocké pour le compte et on le set dans l'accountOther pour pouvoir se connecter via IMAP/SMTP
			accountOther.Password = password;

			var success = await _otherCredentialService.ConnectAsync(accountOther);

			if (!success)
				return;

			await _otherProtocolService.SaveAttachmentAsync(accountOther, messageId, attachment, destinationFolder);

			return;
		}
	}
}
