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

	public async Task AddAddressAsync(AccountGmail? accountGmail = null, AccountOther? accountOther = null)
	{
		using var _context = _factory.CreateDbContext();

		if (accountGmail != null)
		{
			_context.AccountGmail.Add(accountGmail);
		}

		if (accountOther != null)
		{
			_context.AccountOther.Add(accountOther);
		}

		await _context.SaveChangesAsync();
	}

	public async Task<bool> DeleteAddressAsync(AccountGmail? accountGmail = null, AccountOther? accountOther = null)
	{
		using var _context = _factory.CreateDbContext();

		if (accountGmail != null)
		{
			_context.AccountGmail.Remove(accountGmail);
		}

		if (accountOther != null)
		{
			_context.AccountOther.Remove(accountOther);
		}

		await _context.SaveChangesAsync();

		return true;
	}
}
