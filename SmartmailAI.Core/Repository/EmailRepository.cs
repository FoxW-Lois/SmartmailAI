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

	public async Task<List<EmailGmail>> GetAllEmailsAsync()
	{
		using var _context = _factory.CreateDbContext();

		return await _context.EmailGmail.ToListAsync();
	}

	public async Task AddEmailAsync(EmailGmail emailGmail)
	{
		using var _context = _factory.CreateDbContext();

		_context.EmailGmail.Add(emailGmail);
		await _context.SaveChangesAsync();
	}

	public async Task DeleteEmailAsync(EmailGmail emailGmail)
	{
		using var _context = _factory.CreateDbContext();

		_context.EmailGmail.Remove(emailGmail);
		await _context.SaveChangesAsync();
	}

	public async Task DeleteAllEmailsAsync(AccountGmail accountGmail)
	{
		using var _context = _factory.CreateDbContext();

		var emailsToDelete = await _context.EmailGmail
			.Where(e => e.Owner == accountGmail.Email)
			.ToListAsync();

		_context.EmailGmail.RemoveRange(emailsToDelete);
		await _context.SaveChangesAsync();
	}
}
