using Microsoft.EntityFrameworkCore;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.AppDbContext;

public class AppDbContext_Email(DbContextOptions<AppDbContext_Email> options) : DbContext(options)
{
	public DbSet<Email> Email { get; set; }
}
