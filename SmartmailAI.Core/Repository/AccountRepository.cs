using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartmailAI.Core.AppDbContext;
using SmartmailAI.Core.Contracts;
using SmartmailAI.Core.Contracts.Repository;
using SmartmailAI.Core.Contracts.Services.LocalSecurity;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Repository;

public class AccountRepository(IDbContextFactory<AppDbContext_Account> factory, IAesService aesService) : IAccountRepository,
	IEncryptDecryptDatas<Account>
{
	private readonly IDbContextFactory<AppDbContext_Account> _factory = factory;
	private readonly IAesService _aesService = aesService;

	public async Task<Account?> GetAccountByLoginAsync(string login)
	{
		using var _context = _factory.CreateDbContext();

		var account = await _context.Account
			.FirstOrDefaultAsync(a => a.Login == login);

		if (account == null) return null;

		account = await DecryptDataAsync(account);

		return account;
	}

	public async Task<bool> LoginExistsAsync(string login)
	{
		using var _context = _factory.CreateDbContext();

		return await _context.Account
			.AnyAsync(a => a.Login == login);
	}

	public async Task AddAccountAsync(Account account)
	{
		using var _context = _factory.CreateDbContext();

		account = await EncryptDataAsync(account);

		_context.Account.Add(account);
		await _context.SaveChangesAsync();
	}

	public async Task UpdateAccountAsync(Account account)
	{
		using var _context = _factory.CreateDbContext();

		account = await EncryptDataAsync(account);

		_context.Account.Update(account);
		await _context.SaveChangesAsync();
	}

	public async Task DeleteAccountAsync(Account account)
	{
		using var _context = _factory.CreateDbContext();

		account = await EncryptDataAsync(account);

		_context.Account.Remove(account);
		await _context.SaveChangesAsync();
	}

	#region Chiffrement / Déchiffrement

	public async Task<Account> EncryptDataAsync(Account account)
	{
		account.PhoneNumber = await _aesService.EncryptAsync(account.PhoneNumber);

		return account;
	}

	public async Task<Account> DecryptDataAsync(Account account)
	{
		account.PhoneNumber = await _aesService.DecryptAsync(account.PhoneNumber);

		return account;
	}

	#endregion Chiffrement / Déchiffrement
}
