using Microsoft.EntityFrameworkCore;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.AppDbContext;

public class AppDbContext_Email(DbContextOptions<AppDbContext_Email> options) : DbContext(options)
{
	public required DbSet<EmailGmail> EmailGmail { get; set; }
}
