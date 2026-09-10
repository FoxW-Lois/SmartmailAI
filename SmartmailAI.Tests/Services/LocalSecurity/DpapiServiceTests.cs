using System.Security.Cryptography;
using SmartmailAI.Core.Services.LocalSecurity;

namespace SmartmailAI.Tests.Services.LocalSecurity;

public class DpapiServiceTests
{
	[Fact]
	public void Encrypt_ReturnsBase64String_NotEmpty()
	{
		// Arrange
		var svc = new DpapiService();

		// Act
		var cipher = svc.Encrypt("hello-world");

		// Assert
		Assert.False(string.IsNullOrEmpty(cipher));
		// Ensure result is valid Base64
		var decoded = Convert.FromBase64String(cipher);
		Assert.NotNull(decoded);
		Assert.NotEmpty(decoded);
	}

	[Fact]
	public void EncryptAndDecrypt_Roundtrip_ReturnsOriginal()
	{
		// Arrange
		var svc = new DpapiService();
		var plain = "this-is-a-test";

		// Act
		var cipher = svc.Encrypt(plain);
		var round = svc.Decrypt(cipher);

		// Assert
		Assert.Equal(plain, round);
	}

	[Fact]
	public void Decrypt_InvalidBase64_ThrowsFormatException()
	{
		// Arrange
		var svc = new DpapiService();

		// Act / Assert
		Assert.Throws<FormatException>(() => svc.Decrypt("not-base64!!"));
	}

	[Fact]
	public void Decrypt_TamperedData_ThrowsCryptographicException()
	{
		// Arrange
		var svc = new DpapiService();
		var cipher = svc.Encrypt("sensitive-data");

		// Tamper with the underlying bytes so decoding succeeds but unprotect should fail
		var bytes = Convert.FromBase64String(cipher);
		// Flip some bits in the first byte
		bytes[0] ^= 0xFF;
		var tampered = Convert.ToBase64String(bytes);

		// Act / Assert
		Assert.Throws<CryptographicException>(() => svc.Decrypt(tampered));
	}
}
