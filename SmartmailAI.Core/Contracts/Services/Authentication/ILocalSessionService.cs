using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Contracts.Services.Authentication;

public interface ILocalSessionService
{
	void CreateSession();

	// TODO : Mettre en place le RotateSession() avec un serveur distant une fois en production
	//string? RotateSession();

	bool ValidateSession();

	void SaveSession(LocalSession session);

	void KillSession();

	LocalSession? LoadSession();
}
