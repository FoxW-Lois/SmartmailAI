using System.Threading.Tasks;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Contracts.Services.Addresses;

public interface IOtherLogoutService
{
	Task LogoutAsync(AccountOther account);
}
