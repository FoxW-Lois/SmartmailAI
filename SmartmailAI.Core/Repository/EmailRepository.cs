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

	// TODO: Mettre en paramètres : (AccountOutlook? accountOutlook)
	public async Task DeleteAllEmailsAsync(AccountGmail? accountGmail = null, AccountOther? accountOther = null)
	{
		using var _context = _factory.CreateDbContext();

		var emailsToDelete = await _context.Email
			.Where(e => (accountGmail != null && e.Owner == accountGmail.Email) ||
							(accountOther != null && e.Owner == accountOther.Email))
			.ToListAsync();

		_context.Email.RemoveRange(emailsToDelete);
		await _context.SaveChangesAsync();
	}
}
