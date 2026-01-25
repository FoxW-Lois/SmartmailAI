using System;
using OtpNet;
using SmartmailAI.Core.Contracts.Services.Authentication;
using SmartmailAI.Core.Data;

namespace SmartmailAI.Core.Services.Authentication;

public class TotpService : ITotpService
{
	public TotpSecret GenerateSecret()
	{
		var key = KeyGeneration.GenerateRandomKey(20);
		return new TotpSecret(Base32Encoding.ToString(key));
	}

	public bool ValidateCode(string base32Secret, string code)
	{
		var totp = new Totp(Base32Encoding.ToBytes(base32Secret));
		return totp.VerifyTotp(code, out _, new VerificationWindow(1, 1));
	}

	public string GenerateOtpAuthUri(string issuer, string account, string base32Secret)
	{
		issuer = Uri.EscapeDataString(issuer);
		account = Uri.EscapeDataString(account);

		return $"otpauth://totp/{issuer}:{account}" +
				$"?secret={base32Secret}" +
				$"&issuer={issuer}" +
				$"&digits=6";
	}
}
