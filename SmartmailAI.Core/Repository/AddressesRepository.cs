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

	public async Task AddAddressByGoogleAsync(AccountGmail accountGmail)
	{
		using var _context = _factory.CreateDbContext();

		_context.AccountMailBase.Add(accountGmail);

		await _context.SaveChangesAsync();
	}

	public async Task AddAddressByOtherAsync(AccountOther accountOther)
	{
		using var _context = _factory.CreateDbContext();

		_context.AccountMailBase.Add(accountOther);

		await _context.SaveChangesAsync();
	}

	public async Task<bool> DeleteAddressByGoogleAsync(AccountGmail accountGmail)
	{
		using var _context = _factory.CreateDbContext();

		_context.AccountMailBase.Remove(accountGmail);

		await _context.SaveChangesAsync();

		return true;
	}

	public async Task<bool> DeleteAddressByOtherAsync(AccountOther accountOther)
	{
		using var _context = _factory.CreateDbContext();

		_context.AccountMailBase.Remove(accountOther);

		await _context.SaveChangesAsync();

		return true;
	}
}
