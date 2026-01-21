using System.Threading.Tasks;

namespace SmartmailAI.Core.Contracts.Services;

public interface IAuthService
{
	bool IsAuthenticated { get; }


	Task<(bool, string?)> LoginAsync(string login, string password);

	Task<(bool Success, string Error)> RegisterAsync(string login, string phoneNumber, string password);

	void Logout();
}
