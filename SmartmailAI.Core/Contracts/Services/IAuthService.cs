using System;
using System.Threading.Tasks;

namespace SmartmailAI.Core.Contracts.Services;

public interface IAuthService
{
	bool IsAuthenticated { get; }

	// Notification du changement d'état concernant l'authentification de l'utilisateur
	event EventHandler<bool> AuthenticationStateChanged;

	Task<bool> TryRestoreSessionAsync();

	Task<(bool Success, string? SpecificError)> LoginAsync(string login, string password);

	Task<(bool Success, string Error)> RegisterAsync(string login, string phoneNumber, string password);

	void Logout();
}
