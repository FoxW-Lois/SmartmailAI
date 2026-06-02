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

	public async Task<List<Email>> GetAllEmailsAsync()
	{
		using var _context = _factory.CreateDbContext();

		var emails = await _context.Email
		   .OrderByDescending(e => e.DateSent)
		   .ToListAsync();

		emails = await DecryptEmailListAsync(emails);

		return emails;
	}

	public async Task<List<Email>> GetAllEmailsByAddressAsync(string ownerAddress)
	{
		using var _context = _factory.CreateDbContext();

		var emails = await _context.Email
			.OrderByDescending(e => e.DateSent)
			.ToListAsync();

		emails = await DecryptEmailListAsync(emails);
		emails = [.. emails.Where(e => e.Owner == ownerAddress)];

		return emails;
	}

	public async Task AddEmailAsync(Email email)
	{
		using var _context = _factory.CreateDbContext();

		email = await EncryptDataAsync(email);

		_context.Email.Add(email);
		await _context.SaveChangesAsync();
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
		emailsToDelete = [.. emailsToDelete.Where(e => account != null && e.Owner == account.Email)];

		_context.Email.RemoveRange(emailsToDelete);
		await _context.SaveChangesAsync();
	}

	public async Task<IReadOnlyList<Email>> KeepOnlyNewEmailsAsync(string ownerAddress, List<Email> newEmails)
	{
		using var _context = _factory.CreateDbContext();

		newEmails = await EncryptEmailListAsync(newEmails);

		var existingAddresses = await _context.Email.Where(e => e.Owner == ownerAddress).Select(e => e.Guid).ToHashSetAsync();
		var newEmailsToKeep = newEmails.Where(e => !existingAddresses.Contains(e.Guid)).ToList();

		newEmailsToKeep = await DecryptEmailListAsync(newEmailsToKeep);

		return newEmailsToKeep;
	}

	public async Task<Email> EncryptDataAsync(Email email)
	{
		email.SenderEmail = await _aesService.EncryptAsync(email.SenderEmail);
		email.SenderName = await _aesService.EncryptAsync(email.SenderName);
		if (email.ReceiverEmail != null) email.ReceiverEmail = await _aesService.EncryptAsync(email.ReceiverEmail);
		if (email.ReceiverName != null) email.ReceiverName = await _aesService.EncryptAsync(email.ReceiverName);
		if (email.Cc != null) email.Cc = await _aesService.EncryptAsync(email.Cc);
		if (email.Bcc != null) email.Bcc = await _aesService.EncryptAsync(email.Bcc);
		if (email.Subject != null) email.Subject = await _aesService.EncryptAsync(email.Subject);
		if (email.Content != null) email.Content = await _aesService.EncryptAsync(email.Content);

		// TODO: à voir pour le chiffrement de ownerAddress
		//email.Owner = await _aesService.EncryptAsync(email.Owner);

		if (email.Attachments.Count > 0)
		{
			var json = JsonSerializer.Serialize(email.Attachments);
			email.AttachmentsJson = await _aesService.EncryptAsync(json);
		}

		if (email.DetectedLinks != null) email.DetectedLinks = await _aesService.EncryptAsync(email.DetectedLinks);

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
		if (email.ReceiverEmail != null) email.ReceiverEmail = await _aesService.DecryptAsync(email.ReceiverEmail);
		if (email.ReceiverName != null) email.ReceiverName = await _aesService.DecryptAsync(email.ReceiverName);
		if (email.Cc != null) email.Cc = await _aesService.DecryptAsync(email.Cc);
		if (email.Bcc != null) email.Bcc = await _aesService.DecryptAsync(email.Bcc);
		if (email.Subject != null) email.Subject = await _aesService.DecryptAsync(email.Subject);
		if (email.Content != null) email.Content = await _aesService.DecryptAsync(email.Content);

		// TODO: à voir pour le déchiffrement de ownerAddress
		//email.Owner = await _aesService.DecryptAsync(email.Owner);

		if (email.AttachmentsJson != null)
		{
			var json = await _aesService.DecryptAsync(email.AttachmentsJson);
			email.Attachments = JsonSerializer.Deserialize<List<MailAttachment>>(json) ?? [];
		}

		if (email.DetectedLinks != null) email.DetectedLinks = await _aesService.DecryptAsync(email.DetectedLinks);

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
}
