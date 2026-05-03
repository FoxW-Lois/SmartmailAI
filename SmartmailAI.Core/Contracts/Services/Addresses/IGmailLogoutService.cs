using System.Threading.Tasks;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Contracts.Services.Addresses;

public interface IGmailLogoutService
{
	Task LogoutAsync(AccountGmail account);
}
