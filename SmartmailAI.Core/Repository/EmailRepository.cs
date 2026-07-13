using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartmailAI.Core.AppDbContext;
using SmartmailAI.Core.Contracts;
using SmartmailAI.Core.Contracts.Repository;
using SmartmailAI.Core.Contracts.Services.LocalSecurity;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Repository;

public class EmailRepository(IDbContextFactory<AppDbContext_Email> factory, IAesService aesService) : IEmailRepository,
	IEncryptDecryptDatas<Email>
{
	private readonly IDbContextFactory<AppDbContext_Email> _factory = factory;
	private readonly IAesService _aesService = aesService;

	public async Task<(List<Email>, int totalCount)> GetEmailsByAddressAndMailboxTypeAsync(MailboxType mailboxType, string ownerAddress, int page, int pageSize)
	{
		using var _context = _factory.CreateDbContext();

		var query = _context.Email
			.Where(e => e.Owner == ownerAddress);

		query = mailboxType switch
		{
			MailboxType.Inbox => query.Where(e => e.MailboxType == MailboxType.Inbox),
			MailboxType.Sent => query.Where(e => e.MailboxType == MailboxType.Sent || e.SenderEmail == e.ReceiverEmail),
			MailboxType.Drafts => query.Where(e => e.MailboxType == MailboxType.Drafts),
			MailboxType.Starred => query.Where(e => e.IsStarred == true),
			MailboxType.Unread => query.Where(e => e.IsRead == false && e.MailboxType != MailboxType.Trash
				&& e.MailboxType != MailboxType.Archives && e.MailboxType != MailboxType.PhishingSpam),

			MailboxType.Trash => query.Where(e => e.MailboxType == MailboxType.Trash),
			MailboxType.Archives => query.Where(e => e.MailboxType == MailboxType.Archives),
			MailboxType.PhishingSpam => query.Where(e => e.MailboxType == MailboxType.PhishingSpam),

			_ => query.Where(e => e.MailboxType != MailboxType.Drafts && e.MailboxType != MailboxType.Trash
				&& e.MailboxType != MailboxType.Archives && e.MailboxType != MailboxType.PhishingSpam)
		};

		var totalCount = await query.CountAsync();

		var emails = await query
			.OrderByDescending(e => e.DateSent)
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync();

		emails = await DecryptEmailListAsync(emails);
		// TODO: Ajouter chiffrement du Owner et donc un hashage + sel du Owner pour pouvoir faire la recherche par OwnerAddress
		//emails = [.. emails.Where(e => e.Owner == ownerAddress)];

		return (emails, totalCount);
	}

	public async Task AddEmailAsync(Email email)
	{
		using var _context = _factory.CreateDbContext();

		email = await EncryptDataAsync(email);

		_context.Email.Add(email);

		try
		{
			await _context.SaveChangesAsync();
		}
		catch (DbUpdateException)
		{
			// En cas de doublon (email déjà présent en base), on ignore silencieusement et on continue
			// Peut arriver dans le cas où un utilisateur s'est envoyé un email à lui-même, mais cela est normalement déjà ammorcé par
			// MailReaderService avant appel de AddEmailAsync()
		}
	}

	public async Task UpdateEmailAsync(Email email)
	{
		using var _context = _factory.CreateDbContext();

		email = await EncryptDataAsync(email);

		_context.Email.Update(email);
		await _context.SaveChangesAsync();
	}

	public async Task DeleteEmailAsync(Email email)
	{
		using var _context = _factory.CreateDbContext();

		email = await EncryptDataAsync(email);

		_context.Email.Remove(email);
		await _context.SaveChangesAsync();
	}

	public async Task DeleteAllEmailsAsync(AccountMailBase account)
	{
		using var _context = _factory.CreateDbContext();

		var emails = await _context.Email
		   .OrderByDescending(e => e.DateSent)
		   .ToListAsync();

		var emailsToDelete = await EncryptEmailListAsync(emails);
		emailsToDelete = [.. emailsToDelete.Where(e => account is not null && e.Owner == account.Email)];

		_context.Email.RemoveRange(emailsToDelete);
		await _context.SaveChangesAsync();
	}

	public async Task DeleteEmailByGuidAsync(string guid)
	{
		using var _context = _factory.CreateDbContext();

		var email = await _context.Email.FirstOrDefaultAsync(e => e.Guid == guid);
		if (email is null)
			return;

		email = await EncryptDataAsync(email);

		_context.Email.Remove(email);
		await _context.SaveChangesAsync();
	}

	public async Task<IReadOnlyList<Email>> KeepOnlyNewEmailsAsync(string ownerAddress, List<Email> newEmails, bool isFromOtherAddress)
	{
		using var _context = _factory.CreateDbContext();

		newEmails = await EncryptEmailListAsync(newEmails);

		// Fait un Check du Guid sur les nouveaux emails entrants, par rapport à ceux déjà présents en base pour éviter les doublons
		// Dans le cas où l'adresse Email les possédant est connectée au projet via SMTP/IMAP, il faut supprimer le "-nombre" à la fin du Guid
		// mais uniquement dans la comparaison, pas dans les données stockées en base
		HashSet<string>? existingAddresses;
		List<Email>? newEmailsToKeep;

		if (!isFromOtherAddress)
		{
			existingAddresses = await _context.Email.Where(e => e.Owner == ownerAddress).Select(e => e.Guid).ToHashSetAsync();
			newEmailsToKeep = [.. newEmails.Where(e => !existingAddresses.Contains(e.Guid))];
		}
		else
		{
			existingAddresses = [.. (await _context.Email.Where(e => e.Owner == ownerAddress).Select(e => e.Guid).
				ToListAsync()).Select(NormalizeGuid)];
			newEmailsToKeep = [.. newEmails.Where(e => !existingAddresses.Contains(NormalizeGuid(e.Guid)))];
		}

		newEmailsToKeep = await DecryptEmailListAsync(newEmailsToKeep);

		return newEmailsToKeep;
	}

	public string NormalizeGuid(string guid)
	{
		int pos = guid.LastIndexOf('-');
		return pos > 0 ? guid[..pos] : guid;
	}

	#region Chiffrement / Déchiffrement

	public async Task<Email> EncryptDataAsync(Email email)
	{
		email.SenderEmail = await _aesService.EncryptAsync(email.SenderEmail);
		email.SenderName = await _aesService.EncryptAsync(email.SenderName);
		if (email.ReceiverEmail is not null) email.ReceiverEmail = await _aesService.EncryptAsync(email.ReceiverEmail);
		if (email.ReceiverName is not null) email.ReceiverName = await _aesService.EncryptAsync(email.ReceiverName);
		if (email.Cc is not null) email.Cc = await _aesService.EncryptAsync(email.Cc);
		if (email.Bcc is not null) email.Bcc = await _aesService.EncryptAsync(email.Bcc);
		if (email.Subject is not null) email.Subject = await _aesService.EncryptAsync(email.Subject);
		if (email.Content is not null) email.Content = await _aesService.EncryptAsync(email.Content);

		if (email.Attachments.Count > 0)
		{
			var json = JsonSerializer.Serialize(email.Attachments);
			email.AttachmentsJson = await _aesService.EncryptAsync(json);
		}

		if (email.DetectedLinks is not null) email.DetectedLinks = await _aesService.EncryptAsync(email.DetectedLinks);

		return email;
	}

	private async Task<List<Email>> EncryptEmailListAsync(List<Email> emails)
	{
		List<Email> encryptedEmails = [];

		foreach (var email in emails)
		{
			encryptedEmails.Add(await EncryptDataAsync(email));
		}

		return encryptedEmails;
	}

	public async Task<Email> DecryptDataAsync(Email email)
	{
		email.SenderEmail = await _aesService.DecryptAsync(email.SenderEmail);
		email.SenderName = await _aesService.DecryptAsync(email.SenderName);
		if (email.ReceiverEmail is not null) email.ReceiverEmail = await _aesService.DecryptAsync(email.ReceiverEmail);
		if (email.ReceiverName is not null) email.ReceiverName = await _aesService.DecryptAsync(email.ReceiverName);
		if (email.Cc is not null) email.Cc = await _aesService.DecryptAsync(email.Cc);
		if (email.Bcc is not null) email.Bcc = await _aesService.DecryptAsync(email.Bcc);
		if (email.Subject is not null) email.Subject = await _aesService.DecryptAsync(email.Subject);
		if (email.Content is not null) email.Content = await _aesService.DecryptAsync(email.Content);

		if (email.AttachmentsJson is not null)
		{
			var json = await _aesService.DecryptAsync(email.AttachmentsJson);
			email.Attachments = JsonSerializer.Deserialize<List<MailAttachment>>(json) ?? [];
		}

		if (email.DetectedLinks is not null) email.DetectedLinks = await _aesService.DecryptAsync(email.DetectedLinks);

		return email;
	}

	private async Task<List<Email>> DecryptEmailListAsync(List<Email> emails)
	{
		List<Email> decryptedEmails = [];

		foreach (var email in emails)
		{
			decryptedEmails.Add(await DecryptDataAsync(email));
		}

		return decryptedEmails;
	}

	#endregion Chiffrement / Déchiffrement
}
