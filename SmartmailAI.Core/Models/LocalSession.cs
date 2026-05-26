using System;

namespace SmartmailAI.Core.Models;

public sealed class LocalSession
{
	public string SessionId { get; set; } = default!;

	public string CurrentRefreshToken { get; set; } = default!;
	public string CurrentRefreshTokenHash { get; set; } = default!;
	public string PreviousRefreshTokenHash { get; set; } = default!;
	public string MachineFingerprint { get; set; } = default!;

	public DateTimeOffset CreatedUtc { get; set; }
	public DateTimeOffset ExpiresUtc { get; set; }

	public long CreatedUptime { get; set; }
	public int RotationCounter { get; set; }
}
