using Microsoft.EntityFrameworkCore;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.AppDbContext;

public class AppDbContext_Address(DbContextOptions<AppDbContext_Address> options) : DbContext(options)
{
	// Même si utilisés nulle part, la déclaration des DbSet AccountGmail et AccountOther est nécessaire pour EF Core afin qu'il gère
	// la distinction entre les objets de l'un et l'autre en base de données via l'héritage (par la colonne Discriminator)

	public required DbSet<AccountGmail> AccountGmail { get; set; }

	public required DbSet<AccountOther> AccountOther { get; set; }

	public required DbSet<AccountMailBase> AccountMailBase { get; set; }
}
