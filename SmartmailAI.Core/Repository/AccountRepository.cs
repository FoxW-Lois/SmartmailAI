using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartmailAI.Core.Data;
using SmartmailAI.Core.IRepository;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Repository;

public class AccountRepository(IDbContextFactory<AppDbContext_Account> factory) : IAccountRepository
{
	private readonly IDbContextFactory<AppDbContext_Account> _factory = factory;

	public async Task<Account?> GetByLoginAsync(string login)
	{
		using var _context = _factory.CreateDbContext();

		return await _context.Account
			.FirstOrDefaultAsync(a => a.Login == login);
	}

	public async Task<bool> LoginExistsAsync(string login)
	{
		using var _context = _factory.CreateDbContext();

		return await _context.Account
			.AnyAsync(a => a.Login == login);
	}

	public async Task AddAsync(Account account)
	{
		using var _context = _factory.CreateDbContext();

		_context.Account.Add(account);
		await _context.SaveChangesAsync();
	}

	public async Task UpdateAsync(Account account)
	{
		using var _context = _factory.CreateDbContext();

		_context.Account.Update(account);
		await _context.SaveChangesAsync();
	}

	public async Task DeleteAsync(Account account)
	{
		using var _context = _factory.CreateDbContext();

		_context.Account.Remove(account);
		await _context.SaveChangesAsync();
	}
}
