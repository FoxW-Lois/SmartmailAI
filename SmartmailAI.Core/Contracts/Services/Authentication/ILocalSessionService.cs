using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Contracts.Services.Authentication;

public interface ILocalSessionService
{
	void CreateSession();

	string? RotateSession();

	bool ValidateSession();

	void SaveSession(LocalSession session);

	void KillSession();
}
