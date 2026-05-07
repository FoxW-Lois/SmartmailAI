using System.Threading.Tasks;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Contracts.Services.Addresses;

public interface IOutlookLogoutService
{
	Task LogoutAsync(AccountOutlook account);
}
