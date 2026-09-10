using OtpNet;
using SmartmailAI.Core.Services.Authentication;

namespace SmartmailAI.Tests.Services.Authentication;

public class TotpServiceTests
{
	[Fact]
	public void GenerateSecret_ReturnsNonEmptyBase32()
	{
		// Arrange
		var svc = new TotpService();

		// Act
		var secret = svc.GenerateSecret();

		// Assert
		Assert.False(string.IsNullOrEmpty(secret.Base32));
		// Ensure it decodes to bytes using Base32
		var bytes = Base32Encoding.ToBytes(secret.Base32);
		Assert.NotNull(bytes);
		Assert.NotEmpty(bytes);
	}

	[Fact]
	public void ValidateCode_ReturnsTrueForCurrentCode()
	{
		// Arrange
		var svc = new TotpService();
		var secret = svc.GenerateSecret().Base32;

		// Create a TOTP and compute current code
		var totp = new Totp(Base32Encoding.ToBytes(secret));
		var code = totp.ComputeTotp();

		// Act
		var valid = svc.ValidateCode(secret, code);

		// Assert
		Assert.True(valid);
	}

	[Fact]
	public void ValidateCode_ReturnsFalseForCodeFromDifferentSecret()
	{
		// Arrange
		var svc = new TotpService();
		var secret1 = svc.GenerateSecret().Base32;
		var secret2 = svc.GenerateSecret().Base32;

		var totp2 = new Totp(Base32Encoding.ToBytes(secret2));
		var code2 = totp2.ComputeTotp();

		// Act
		var valid = svc.ValidateCode(secret1, code2);

		// Assert
		Assert.False(valid);
	}

	[Fact]
	public void GenerateOtpAuthUri_FormatsCorrectly_WithEscaping()
	{
		// Arrange
		var svc = new TotpService();
		var issuer = "My Issuer/Name";
		var account = "user+test@example.com";
		var base32 = "JBSWY3DPEHPK3PXP"; // known base32 sample

		// Act
		var uri = svc.GenerateOtpAuthUri(issuer, account, base32);

		// Assert
		var escIssuer = Uri.EscapeDataString(issuer);
		var escAccount = Uri.EscapeDataString(account);
		var expected = $"otpauth://totp/{escIssuer}:{escAccount}?secret={base32}&issuer={escIssuer}&digits=6";
		Assert.Equal(expected, uri);
	}
}
