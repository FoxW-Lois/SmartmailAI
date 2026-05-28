using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartmailAI.Core.AppDbContext;
using SmartmailAI.Core.Contracts.Repository;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Repository;

public class EmailRepository(IDbContextFactory<AppDbContext_Email> factory) : IEmailRepository
{
	private readonly IDbContextFactory<AppDbContext_Email> _factory = factory;

	public async Task<List<Email>> GetAllEmailsAsync()
	{
		using var _context = _factory.CreateDbContext();

		return await _context.Email
			.OrderByDescending(e => e.DateSent)
			.ToListAsync();
	}

	public async Task<List<Email>> GetAllEmailsByAddressAsync(string ownerAddress)
	{
		using var _context = _factory.CreateDbContext();

		return await _context.Email
			.Where(e => e.Owner == ownerAddress)
			.OrderByDescending(e => e.DateSent)
			.ToListAsync();
	}

	public async Task AddEmailAsync(Email email)
	{
		using var _context = _factory.CreateDbContext();

		_context.Email.Add(email);
		await _context.SaveChangesAsync();
	}

	public async Task UpdateEmailAsync(Email email)
	{
		using var _context = _factory.CreateDbContext();

		_context.Email.Update(email);
		await _context.SaveChangesAsync();
	}

	public async Task DeleteEmailAsync(Email email)
	{
		using var _context = _factory.CreateDbContext();

		_context.Email.Remove(email);
		await _context.SaveChangesAsync();
	}

	public async Task DeleteAllEmailsAsync(AccountMailBase account)
	{
		using var _context = _factory.CreateDbContext();

		List<Email>? emailsToDelete = await _context.Email.Where(e => account != null && e.Owner == account.Email).ToListAsync();

		_context.Email.RemoveRange(emailsToDelete);
		await _context.SaveChangesAsync();
	}

	public async Task<IReadOnlyList<Email>> KeepOnlyNewEmailsAsync(string ownerAddress, List<Email> newEmails)
	{
		using var _context = _factory.CreateDbContext();

		List<Email> existingEmails = await _context.Email.Where(e => e.Owner == ownerAddress).ToListAsync();

		var existingAddresses = await _context.Email.Where(e => e.Owner == ownerAddress).Select(e => e.Guid).ToHashSetAsync();
		var newEmailsToKeep = newEmails.Where(e => !existingAddresses.Contains(e.Guid)).ToList();

		return newEmailsToKeep;
	}
}
