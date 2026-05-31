using Microsoft.EntityFrameworkCore;
using SmartmailAI.Core.Contracts.Services.LocalSecurity;
using SmartmailAI.Core.Data;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.AppDbContext;

public class AppDbContext_Email(DbContextOptions<AppDbContext_Email> options, IAesService aesService) : DbContext(options)
{
	private readonly IAesService _aesService = aesService;

	public DbSet<Email> Email { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		var converter = new AesValueConverter(_aesService);

		modelBuilder.Entity<Email>().Property(e => e.Subject).HasConversion(converter!);

		modelBuilder.Entity<Email>().Property(e => e.Content).HasConversion(converter!);
	}
}
