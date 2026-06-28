using Microsoft.EntityFrameworkCore;
using SmartmailAI.Core.Models.Security;

namespace SmartmailAI.Core.AppDbContext;

public class AppDbContext_MLDA(DbContextOptions<AppDbContext_MLDA> options) : DbContext(options) // MLDA => ManualLegitDomainsAndAddresses
{
	public DbSet<ManualLegitDomainsAndAddresses> ManualLegitDomainsAndAddresses { get; set; }
}
