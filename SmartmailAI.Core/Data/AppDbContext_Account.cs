using SmartmailAI.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace SmartmailAI.Core.Data;

public class AppDbContext_Account(DbContextOptions<AppDbContext_Account> options) : DbContext(options)
{
	public required DbSet<Account> Account { get; set; }
}
