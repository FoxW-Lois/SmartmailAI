using Moq;
using SmartmailAI.Core.Contracts.Services.LocalSecurity;
using SmartmailAI.Core.Services.LocalSecurity;

namespace SmartmailAI.Tests.Services.LocalSecurity;

public class AesServiceTests
{
	[Fact]
	public async Task EncryptThenDecrypt_ReturnsOriginalPlainText()
	{
		// Arrange
		var key = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray(); // 256-bit key
		var aesKeyMock = new Mock<IAesKeyService>();
		aesKeyMock.Setup(k => k.GetOrCreateKeyAsync()).ReturnsAsync(key);

		var service = new AesService(aesKeyMock.Object);
		var plain = "hello@example.com";

		// Act
		var cipher = await service.EncryptAsync(plain);
		var decrypted = await service.DecryptAsync(cipher);

		// Assert
		Assert.Equal(plain, decrypted);
	}

	[Fact]
	public async Task EncryptAsync_ProducesDifferentCiphertextsForSamePlaintext()
	{
		// Arrange
		var key = Enumerable.Range(0, 32).Select(i => (byte)(i + 1)).ToArray(); // 256-bit key
		var aesKeyMock = new Mock<IAesKeyService>();
		aesKeyMock.Setup(k => k.GetOrCreateKeyAsync()).ReturnsAsync(key);

		var service = new AesService(aesKeyMock.Object);
		var plain = "repeat@example.com";

		// Act
		var cipher1 = await service.EncryptAsync(plain);
		var cipher2 = await service.EncryptAsync(plain);

		// Assert
		Assert.NotNull(cipher1);
		Assert.NotNull(cipher2);
		Assert.NotEqual(cipher1, cipher2); // different IVs should produce different ciphertexts

		// Also ensure both decrypt back to the same plain text
		var dec1 = await service.DecryptAsync(cipher1);
		var dec2 = await service.DecryptAsync(cipher2);
		Assert.Equal(plain, dec1);
		Assert.Equal(plain, dec2);
	}

	[Fact]
	public async Task GetOrCreateKeyAsync_IsCalledOnlyOnce_DueToKeyCaching()
	{
		// Arrange
		var key = Enumerable.Range(0, 32).Select(i => (byte)(i + 2)).ToArray();
		var aesKeyMock = new Mock<IAesKeyService>();
		aesKeyMock.Setup(k => k.GetOrCreateKeyAsync()).ReturnsAsync(key);

		var service = new AesService(aesKeyMock.Object);
		var plain = "cache@test.com";

		// Act
		var cipher = await service.EncryptAsync(plain);
		var decrypted = await service.DecryptAsync(cipher);

		// Assert
		Assert.Equal(plain, decrypted);
		aesKeyMock.Verify(k => k.GetOrCreateKeyAsync(), Times.Once);
	}
}
