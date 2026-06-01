using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartmailAI.Core.AppDbContext;
using SmartmailAI.Core.Contracts;
using SmartmailAI.Core.Contracts.Repository;
using SmartmailAI.Core.Contracts.Services.LocalSecurity;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Repository;

public class AddressesRepository(IDbContextFactory<AppDbContext_Address> factory, IAesService aesService) : IAddressesRepository,
	IEncryptDecryptDatas<AccountMailBase>
{
	private readonly IDbContextFactory<AppDbContext_Address> _factory = factory;
	private readonly IAesService _aesService = aesService;

	public async Task<List<AccountMailBase>> GetAllAddressesAsync()
	{
		using var _context = _factory.CreateDbContext();

		var addresses = await _context.AccountMailBase.ToListAsync();

		addresses = await DecryptAddressListAsync(addresses);

		return addresses;
	}

	public async Task<AccountMailBase?> GetAddressByEmailAsync(string email)
	{
		using var _context = _factory.CreateDbContext();

		var addresses = await _context.AccountMailBase.ToListAsync();

		addresses = await DecryptAddressListAsync(addresses);
		AccountMailBase? address = addresses.FirstOrDefault(a => a.Email == email);

		return address;
	}

	public async Task AddAddressAsync(AccountMailBase account)
	{
		using var _context = _factory.CreateDbContext();

		account = await EncryptDataAsync(account);

		_context.AccountMailBase.Add(account);
		await _context.SaveChangesAsync();
	}

	public async Task DeleteAddressAsync(AccountMailBase account)
	{
		using var _context = _factory.CreateDbContext();

		account = await EncryptDataAsync(account);

		_context.AccountMailBase.Remove(account);
		await _context.SaveChangesAsync();
	}

	public async Task<AccountMailBase> EncryptDataAsync(AccountMailBase account)
	{
		account.Email = await _aesService.EncryptAsync(account.Email);
		account.TokenStorageKey = await _aesService.EncryptAsync(account.TokenStorageKey);

		if (account is AccountGmail accountGmail)
		{
			accountGmail.GoogleUserId = await _aesService.EncryptAsync(accountGmail.GoogleUserId);
		}
		else if (account is AccountOther accountOther)
		{
			accountOther.UserName = await _aesService.EncryptAsync(accountOther.ImapHost);
			accountOther.ImapHost = await _aesService.EncryptAsync(accountOther.ImapHost);
			accountOther.SmtpHost = await _aesService.EncryptAsync(accountOther.SmtpHost);
		}

		return account;
	}

	public async Task<AccountMailBase> DecryptDataAsync(AccountMailBase account)
	{
		account.Email = await _aesService.DecryptAsync(account.Email);
		account.TokenStorageKey = await _aesService.DecryptAsync(account.TokenStorageKey);

		if (account is AccountGmail accountGmail)
		{
			accountGmail.GoogleUserId = await _aesService.DecryptAsync(accountGmail.GoogleUserId);
		}
		else if (account is AccountOther accountOther)
		{
			accountOther.UserName = await _aesService.DecryptAsync(accountOther.ImapHost);
			accountOther.ImapHost = await _aesService.DecryptAsync(accountOther.ImapHost);
			accountOther.SmtpHost = await _aesService.DecryptAsync(accountOther.SmtpHost);
		}

		return account;
	}

	public async Task<List<AccountMailBase>> DecryptAddressListAsync(List<AccountMailBase> accounts)
	{
		List<AccountMailBase> decryptedAccounts = [];

		foreach (var account in accounts)
		{
			decryptedAccounts.Add(await DecryptDataAsync(account));
		}

		return decryptedAccounts;
	}
}
