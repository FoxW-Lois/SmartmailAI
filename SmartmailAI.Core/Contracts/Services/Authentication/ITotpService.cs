using SmartmailAI.Core.Data;

namespace SmartmailAI.Core.Contracts.Services.Authentication;

public interface ITotpService
{
	TotpSecret GenerateSecret();

	bool ValidateCode(string base32Secret, string code);

	string GenerateOtpAuthUri(string issuer, string account, string base32Secret);
}
