using System.Threading.Tasks;
using Microsoft.Identity.Client;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Contracts.Services.Addresses;

public interface IOutlookCredentialService
{
	Task<AuthenticationResult?> ConnectAsync();

	Task<AuthenticationResult?> GetCredentialAsync(AccountOutlook account);
}
