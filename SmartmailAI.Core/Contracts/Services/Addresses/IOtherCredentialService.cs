using System.Threading.Tasks;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Contracts.Services.Addresses;

public interface IOtherCredentialService
{
	Task<bool> ConnectAsync(AccountOther account);
}
