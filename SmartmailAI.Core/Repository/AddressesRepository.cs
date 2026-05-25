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

	public async Task<List<AccountMailBase>> GetAllAddressesAsync()
	{
		using var _context = _factory.CreateDbContext();

		return await _context.AccountMailBase.ToListAsync();
	}

	public async Task<AccountMailBase?> GetAddressByEmailAsync(string email)
	{
		using var _context = _factory.CreateDbContext();

		return await _context.AccountMailBase
			.FirstOrDefaultAsync(a => a.Email == email);
	}

	public async Task AddAddressAsync(AccountMailBase account)
	{
		using var _context = _factory.CreateDbContext();

		_context.AccountMailBase.Add(account);

		await _context.SaveChangesAsync();
	}

	public async Task<bool> DeleteAddressAsync(AccountMailBase account)
	{
		using var _context = _factory.CreateDbContext();

		_context.AccountMailBase.Remove(account);

		await _context.SaveChangesAsync();

		return true;
	}
}
