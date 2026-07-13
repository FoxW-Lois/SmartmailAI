using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmartmailAI.Core.Contracts.Repository;
using SmartmailAI.Core.Contracts.Services;
using SmartmailAI.Core.Contracts.Services.Addresses;
using SmartmailAI.Core.Contracts.Services.Authentication;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Services.Addresses;

public class MailReaderService(IEmailRepository emailRepository, IGmailCredentialService gmailCredentialService, IGmailApiService gmailApiService,
	IOtherCredentialService otherCredentialService, IOtherProtocolService otherProtocolService, IOtherTokenStore otherTokenStore,
	IAuthService authService, IAccountRepository accountRepository, IAccountService accountService, IAddressesRepository addressesRepository,
	IMappersToEmailDTOService mappersToEmailDTOService) : IMailReaderService
{
	private readonly IEmailRepository _emailRepository = emailRepository;
	private readonly IGmailCredentialService _gmailCredentialService = gmailCredentialService;
	private readonly IGmailApiService _gmailApiService = gmailApiService;

	private readonly IOtherCredentialService _otherCredentialService = otherCredentialService;
	private readonly IOtherProtocolService _otherProtocolService = otherProtocolService;
	private readonly IOtherTokenStore _otherTokenStore = otherTokenStore;

	private readonly IAuthService _authService = authService;
	private readonly IAccountRepository _accountRepository = accountRepository;
	private readonly IAccountService _accountService = accountService;
	private readonly IAddressesRepository _addressesRepository = addressesRepository;
	private readonly IMappersToEmailDTOService _mappersToEmailDTOService = mappersToEmailDTOService;

	public async Task<IReadOnlyList<Email>?> GetLastMessagesFromAccountAsync(bool isAddingNewAddress, AccountMailBase mailAccount)
	{
		if (!await InternetCheckService.HasInternetConnectionAsync())
		{
			// Il est strictement interdit (et impossible) de gérer l'affichage d'une erreur de manque de connexion internet au sein du sous-projet
			// .Core, l'abscence de connexion de internet est donc remontée par le 'null' aux couches supérieurs appellant
			// GetLastMessagesFromAccountAsync(). Ces dernières doivent elles gérer l'affichage du message d'erreur
			return null;
		}

		int? NumMails;
		var account = await _accountService.GetAccountByLoginInLocalSessionAsync();

		if (account is null || account.IsFirstConnection is true)
			return [];

		if (mailAccount.IsFirstConnection)
			NumMails = null; // MaxResults sera au max (donc 300 dans notre cas)
		else
		{
			int averageEmailsPerDay = account.AverageDailyTrafic switch
			{
				"1 à 30 mails par jour" => 30,
				"30 à 60 mails par jour" => 60,
				"60 à 90 mails par jour" => 90,
				"+ de 90 mails par jour" => 150,
				_ => 30
			};

			NumMails = account.NbOpenAppByWeek switch
			{
				< 7 => averageEmailsPerDay * 7,
				>= 7 and < 14 => averageEmailsPerDay,
				>= 14 => (int)Math.Ceiling(averageEmailsPerDay / (((int)account.NbOpenAppByWeek! / 7) * 1.5)),
				_ => averageEmailsPerDay
			};
		}

		DateTime? lastConnection = account.RetrievedAllEmails is true ? new DateTime(2000, 1, 1) : account.DatePicked!.Value.ToDateTime(TimeOnly.MinValue);
		lastConnection = mailAccount.IsFirstConnection ? lastConnection : await GetCurrentAccountLastConnectionAsync();

		List<EmailFromAddress>? rawEmails;

		if (mailAccount is AccountGmail accountGmail)
			rawEmails = await GetGmailMessagesAsync(accountGmail, isAddingNewAddress, NumMails, lastConnection);
		else if (mailAccount is AccountOther accountOther)
			rawEmails = await GetOtherMessagesAsync(accountOther, isAddingNewAddress, NumMails, lastConnection);
		else
			return [];
		// TODO: ajouter un check mailAccount is AccountOutlook accountOutlook

		if (rawEmails is null) // Peut arriver si il y a une perte de connexion pendant la récupération d'emails, ou lorsque le réseau est lent
			return null;

		List<Email> emailsRecovered = await _mappersToEmailDTOService.MapEmailFromAddressToEmail_List(rawEmails);

		if (isAddingNewAddress)
			return emailsRecovered;

		// Le dernier paramètre (isFromOtherAddress) sera true si c'est un OtherAddress (donc SMTP/IMAP)
		var newEmails = await _emailRepository.KeepOnlyNewEmailsAsync(mailAccount.Email, emailsRecovered, mailAccount is AccountOther);

		if (mailAccount.IsFirstConnection is true)
		{
			mailAccount.IsFirstConnection = false;
			await _addressesRepository.UpdateAddressAsync(mailAccount);
		}

		return newEmails;
	}

	#region Getting emails helpers

	private async Task<List<EmailFromAddress>?> GetGmailMessagesAsync(AccountGmail account, bool isAddingNewAddress, int? numMails, DateTime? lastConnection)
	{
		var credential = await _gmailCredentialService.GetCredentialAsync(account, isAddingNewAddress);

		if (credential is null)
			return [];

		var inboxTask = _gmailApiService.GetLastMessagesAsync(credential, "Inbox", isAddingNewAddress, numMails,
			lastConnection);

		var sentTask = _gmailApiService.GetLastMessagesAsync(credential, "Sent", isAddingNewAddress, numMails,
			lastConnection);

		// Fait un Check du Guid sur les 2 listes de nouveaux emails entrants, et si un email de la liste "Sent" a un Guid déjà présent
		// dans la liste "Inbox", alors on le supprime de la liste "Sent" pour éviter les doublons.
		// Cela arrive dans le cas où un email est envoyé à soi-même

		if (await inboxTask is null || await sentTask is null)
			return null;

		var inbox = await inboxTask;
		var sent = await sentTask;

		sent = [.. (await sentTask)!.Where(s => !((inbox!).Select(i => i.Guid)
			.ToHashSet()).Contains(s.Guid))];
		sentTask = Task.FromResult(sent)!;

		await Task.WhenAll(inboxTask, sentTask);

		return [.. (await inboxTask)!, .. (await sentTask)!];
	}

	private async Task<List<EmailFromAddress>?> GetOtherMessagesAsync(AccountOther account, bool isAddingNewAddress, int? numMails, DateTime? lastConnection)
	{
		var connected = await PrepareOtherAccountAsync(account, isAddingNewAddress);

		if (!connected)
			return [];

		var inboxTask = _otherProtocolService.GetLastMessagesAsync(account, "Inbox", isAddingNewAddress, numMails,
			lastConnection);

		var sentTask = _otherProtocolService.GetLastMessagesAsync(account, "Sent", isAddingNewAddress, numMails,
			lastConnection);

		// Fait un Check du Guid sur les 2 listes de nouveaux emails entrants, et si un email de la liste "Sent" a un Guid déjà présent
		// dans la liste "Inbox", alors on le supprime de la liste "Sent" pour éviter les doublons. Et pour cela il faut supprimer le "-nombre"
		// à la fin du Guid mais uniquement dans la comparaison, pas dans les données stockées dans les Listes.
		// Cela arrive dans le cas où un email est envoyé à soi-même

		if (inboxTask is null || sentTask is null)
			return null;

		var inbox = await inboxTask;
		var sent = await sentTask;

		sent = [.. (await sentTask)!.Where(s => !((inbox!).Select(i => _emailRepository.NormalizeGuid(i.Guid))
			.ToHashSet()).Contains(_emailRepository.NormalizeGuid(s.Guid)))];
		sentTask = Task.FromResult(sent)!;

		await Task.WhenAll(inboxTask, sentTask);

		return [.. (await inboxTask)!, .. (await sentTask)!];
	}

	#endregion Getting emails helpers

	public async Task SaveAttachmentFromEmailAsync(string messageId, MailAttachment attachment, string destinationFolder, AccountMailBase mailAccount)
	{
		if (mailAccount is AccountGmail accountGmail)
		{
			var credential = await _gmailCredentialService.GetCredentialAsync(accountGmail, false);

			if (credential is null)
				return;

			await _gmailApiService.SaveAttachmentAsync(credential, messageId, attachment, destinationFolder);

			return;
		}

		if (mailAccount is AccountOther accountOther)
		{
			var connected = await PrepareOtherAccountAsync(accountOther, false);

			if (!connected)
				return;

			await _otherProtocolService.SaveAttachmentAsync(accountOther, messageId, attachment, destinationFolder);
		}
	}

	#region Other account helpers

	private async Task<bool> PrepareOtherAccountAsync(AccountOther account, bool isCrypted)
	{
		AccountOther decryptedAccountOther = account;

		if (isCrypted)
			decryptedAccountOther = (AccountOther)await _addressesRepository.DecryptDataAsync(decryptedAccountOther);

		string? password = await _otherTokenStore.GetPasswordAsync(account.TokenStorageKey);

		if (password is null)
			return false;

		decryptedAccountOther.Password = password;

		return await _otherCredentialService.ConnectAsync(decryptedAccountOther);
	}

	private async Task<DateTime?> GetCurrentAccountLastConnectionAsync()
	{
		string currentAccountLogin = _authService.CurrentAccountLogin;

		var currentAccount = await _accountRepository.GetAccountByLoginAsync(currentAccountLogin);

		return currentAccount?.LastConnection;
	}

	#endregion Other account helpers
}
