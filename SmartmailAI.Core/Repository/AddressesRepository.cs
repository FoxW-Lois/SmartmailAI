using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartmailAI.Core.AppDbContext;
using SmartmailAI.Core.Contracts.Repository;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Repository;

public class AddressesRepository(IDbContextFactory<AppDbContext_Address> factory) : IAddressesRepository
{
	private readonly IDbContextFactory<AppDbContext_Address> _factory = factory;

	public async Task<List<AccountGmail>> GetAllAddressAsync()
	{
		using var _context = _factory.CreateDbContext();

		return await _context.AccountGmail.ToListAsync();
	}

	public async Task<AccountGmail?> GetByEmailAsync(string email)
	{
		using var _context = _factory.CreateDbContext();

		return await _context.AccountGmail
			.FirstOrDefaultAsync(a => a.Email == email);
	}

	public async Task AddAsync(AccountGmail accountGmail)
	{
		using var _context = _factory.CreateDbContext();

		_context.AccountGmail.Add(accountGmail);
		await _context.SaveChangesAsync();
	}

	public async Task<bool> DeleteAsync(AccountGmail accountGmail)
	{
		using var _context = _factory.CreateDbContext();

		_context.AccountGmail.Remove(accountGmail);
		await _context.SaveChangesAsync();

		return true;
	}
}
