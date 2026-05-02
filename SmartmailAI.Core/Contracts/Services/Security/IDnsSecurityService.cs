using System.Threading.Tasks;
using SmartmailAI.Core.Models.Security;

namespace SmartmailAI.Core.Contracts.Services.Security;

public interface IDnsSecurityService
{
	// Vérifie les enregistrements SPF et DMARC du domaine expéditeur.
	Task<DnsSecurityResult> CheckDomainAsync(string senderEmail);
}
