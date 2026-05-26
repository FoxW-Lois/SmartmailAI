using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using SmartmailAI.Core.Contracts.Services;
using SmartmailAI.Core.Contracts.Services.Authentication;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Services.Authentication;

public class LocalSessionService(IEmailsSyncService emailsSyncService, IAuthService authService) : ILocalSessionService
{
	private readonly IEmailsSyncService _emailsSyncService = emailsSyncService;
	private readonly IAuthService _authService = authService;

	private static readonly string _rootFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
		"SmartmailAI");

	#region Token Helpers

	private static string GenerateToken()
	{
		byte[] bytes = RandomNumberGenerator.GetBytes(64);
		return Convert.ToBase64String(bytes);
	}

	private static string HashToken(string token)
	{
		byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));

		return Convert.ToHexString(bytes);
	}

	#endregion Token Helpers

	#region Session Management

	public void CreateSession()
	{
		string refreshToken = GenerateToken();

		var session = new LocalSession
		{
			SessionId = Guid.NewGuid().ToString(),

			CurrentRefreshToken = refreshToken,
			CurrentRefreshTokenHash = HashToken(refreshToken),
			PreviousRefreshTokenHash = "",
			MachineFingerprint = ComputeFingerprint(),

			CreatedUtc = DateTimeOffset.UtcNow,
			ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30),
			CreatedUptime = Environment.TickCount64,

			RotationCounter = 0
		};

		SaveSession(session);
	}

	// TODO : Mettre en place le RotateSession() avec un serveur distant une fois en production
	//public string? RotateSession()
	//{
	//	LocalSession? session = LoadSession();

	//	if (session is null)
	//		return null;

	//	if (!ValidateSession())
	//	{
	//		_emailsSyncService.Stop();
	//		_authService.Logout();
	//		KillSession();

	//		return null;
	//	}

	//	string newToken = GenerateToken();

	//	session.PreviousRefreshTokenHash = session.CurrentRefreshTokenHash;
	//	session.CurrentRefreshToken = newToken;
	//	session.CurrentRefreshTokenHash = HashToken(newToken);
	//	session.RotationCounter++;
	//	session.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30);

	//	SaveSession(session);

	//	return newToken;
	//}

	public bool ValidateSession()
	{
		LocalSession? session = LoadSession();

		if (session is null)
			return false;

		if (!ValidateFingerprint(session))
			return false;

		if (IsClockTampered(session))
			return false;

		if (DateTimeOffset.UtcNow > session.ExpiresUtc)
			return false;

		string providedHash = HashToken(session.CurrentRefreshToken);

		if (providedHash == session.PreviousRefreshTokenHash) // Ancien token réutilisé => suspect
			return false;

		if (providedHash != session.CurrentRefreshTokenHash)
			return false;

		return true;
	}

	public void SaveSession(LocalSession session)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(_rootFolder)!);

		string json = JsonSerializer.Serialize(session);

		byte[] raw = Encoding.UTF8.GetBytes(json);
		byte[] encrypted = ProtectedData.Protect(raw, null, DataProtectionScope.CurrentUser);
		string tempPath = Path.Combine(_rootFolder, ".tmp");

		File.WriteAllBytes(tempPath, encrypted);
	}

	public void KillSession()
	{
		string sessionPath = Path.Combine(_rootFolder, ".tmp");

		if (File.Exists(sessionPath))
		{
			File.Delete(sessionPath);
		}
	}

	private static LocalSession? LoadSession()
	{
		string sessionPath = Path.Combine(_rootFolder, ".tmp");

		if (!File.Exists(sessionPath))
			return null;

		try
		{
			byte[] encrypted = File.ReadAllBytes(sessionPath);
			byte[] raw = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
			string json = Encoding.UTF8.GetString(raw);

			return JsonSerializer.Deserialize<LocalSession>(json);
		}
		catch
		{
			return null;
		}
	}

	#endregion Session Management

	#region Fingerprint Helpers

	private static bool ValidateFingerprint(LocalSession session)
	{
		return session.MachineFingerprint == ComputeFingerprint();
	}

	private static string ComputeFingerprint()
	{
		string machine = Environment.MachineName;

		string sid = WindowsIdentity.GetCurrent().User?.Value ?? "";
		string combined = $"{machine}|{sid}";
		byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(combined));

		return Convert.ToHexString(hash);
	}

	#endregion Fingerprint Helpers

	private static bool IsClockTampered(LocalSession session)
	{
		long currentUptime = Environment.TickCount64;

		if (currentUptime < session.CreatedUptime)
		{
			// Si reboot machine => éventuellement acceptable
			return false;
		}

		TimeSpan elapsedReal = DateTimeOffset.UtcNow - session.CreatedUtc;
		TimeSpan elapsedUptime = TimeSpan.FromMilliseconds(currentUptime - session.CreatedUptime);

		// Si trop d'écart => suspect
		return Math.Abs((elapsedReal - elapsedUptime).TotalMinutes) > 5;
	}
}
