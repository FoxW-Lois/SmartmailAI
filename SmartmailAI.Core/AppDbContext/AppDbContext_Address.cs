using Microsoft.EntityFrameworkCore;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.AppDbContext;

public class AppDbContext_Address(DbContextOptions<AppDbContext_Address> options) : DbContext(options)
{
	public required DbSet<AccountGmail> AccountGmail { get; set; }

	public required DbSet<AccountOther> AccountOther { get; set; }

	public required DbSet<AccountMailBase> AccountMailBase { get; set; }
}
