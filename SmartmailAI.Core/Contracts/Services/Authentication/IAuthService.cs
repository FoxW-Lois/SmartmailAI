using System;
using System.Threading.Tasks;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Contracts.Services.Authentication;

public interface IAuthService
{
	bool IsAuthenticated { get; }

	string CurrentAccountLogin { get; }

	// Notification du changement d'état concernant l'authentification de l'utilisateur
	event EventHandler<bool> AuthenticationStateChanged;

	Task<bool> TryRestoreSessionAsync();

	Task<(bool Success, string? SpecificError)> LoginAsync(string login, string password);

	Task<(bool Success, string Error)> RegisterAsync(string login, string phoneNumber, string password);

	void Logout();

	Task UpdateLastConnection();

	Task Update_EnableDisable_TwoFactorAsync(string login, bool setEnable);

	Task<bool> ValidateSecondFactorAsync(string login, string code);
}
