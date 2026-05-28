using Microsoft.EntityFrameworkCore;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.AppDbContext;

public class AppDbContext_Email(DbContextOptions<AppDbContext_Email> options) : DbContext(options)
{
	public DbSet<Email> Email { get; set; }

	// TODO : Ajouter/définir une clé primaire composite

	//protected override void OnModelCreating(ModelBuilder modelBuilder)
	//{
	//	base.OnModelCreating(modelBuilder);

	//	modelBuilder.Entity<Email>()
	//		.HasKey(x => new { x.Guid, x.SenderEmail, x.ReceiverEmail }); // Clé primaire composite
	//}
}
