using Moq;
using SmartmailAI.Core.Contracts.Services.Authentication;
using SmartmailAI.Core.Services.Authentication;

namespace SmartmailAI.Tests.Services.Authentication;

public class LocalSessionServiceTests
{
	private static string GetSessionFilePath()
	{
		var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartmailAI");
		return Path.Combine(root, ".tmp");
	}

	private static void DeleteSessionFile()
	{
		var path = GetSessionFilePath();
		if (File.Exists(path))
			File.Delete(path);
	}

	[Fact]
	public void CreateSession_CreatesFileAndSavesSession()
	{
		// Arrange
		DeleteSessionFile();

		var authMock = new Mock<IAuthService>();
		authMock.SetupProperty(a => a.CurrentAccountLogin, "test-user");

		var service = new LocalSessionService(authMock.Object);

		// Act
		service.CreateSession();
		var session = service.LoadSession();

		// Assert
		Assert.NotNull(session);
		Assert.Equal("test-user", session!.Login);
		Assert.False(string.IsNullOrEmpty(session.CurrentRefreshToken));
		Assert.False(string.IsNullOrEmpty(session.CurrentRefreshTokenHash));

		// Cleanup
		service.KillSession();
	}

	[Fact]
	public void ValidateSession_ReturnsTrue_ForValidSessionAndUpdatesAuth()
	{
		// Arrange
		DeleteSessionFile();

		var authMock = new Mock<IAuthService>();
		authMock.SetupProperty(a => a.CurrentAccountLogin);
		authMock.Setup(a => a.UpdateLastConnection());

		var service = new LocalSessionService(authMock.Object);

		service.CreateSession();

		// Act
		bool valid = service.ValidateSession();

		// Assert
		Assert.True(valid);
		authMock.Verify(a => a.UpdateLastConnection(), Times.Once);

		// Cleanup
		service.KillSession();
	}

	[Fact]
	public void ValidateSession_ReturnsFalse_WhenTokenTampered()
	{
		// Arrange
		DeleteSessionFile();

		var authMock = new Mock<IAuthService>();
		authMock.SetupProperty(a => a.CurrentAccountLogin, "u");

		var service = new LocalSessionService(authMock.Object);

		service.CreateSession();

		var session = service.LoadSession();
		Assert.NotNull(session);

		// Tamper token without updating its stored hash
		session!.CurrentRefreshToken = "tampered-token";
		service.SaveSession(session);

		// Act
		bool valid = service.ValidateSession();

		// Assert
		Assert.False(valid);

		// Cleanup
		service.KillSession();
	}

	[Fact]
	public void KillSession_RemovesSessionFile()
	{
		// Arrange
		DeleteSessionFile();

		var authMock = new Mock<IAuthService>();
		authMock.SetupProperty(a => a.CurrentAccountLogin, "u");

		var service = new LocalSessionService(authMock.Object);

		service.CreateSession();

		// Act
		service.KillSession();
		var loaded = service.LoadSession();

		// Assert
		Assert.Null(loaded);
	}
}
