using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartmailAI.Core.AppDbContext;
using SmartmailAI.Core.IRepository;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Repository;

public class EmailRepository(IDbContextFactory<AppDbContext_Email> factory) : IEmailRepository
{
	private readonly IDbContextFactory<AppDbContext_Email> _factory = factory;

	public async Task<List<EmailGmail>> GetAllAddressAsync()
	{
		using var _context = _factory.CreateDbContext();

		return await _context.EmailGmail.ToListAsync();
	}

	public async Task AddAsync(EmailGmail emailGmail)
	{
		using var _context = _factory.CreateDbContext();

		_context.EmailGmail.Add(emailGmail);
		await _context.SaveChangesAsync();
	}

	public async Task DeleteAsync(EmailGmail emailGmail)
	{
		using var _context = _factory.CreateDbContext();

		_context.EmailGmail.Remove(emailGmail);
		await _context.SaveChangesAsync();
	}
}
