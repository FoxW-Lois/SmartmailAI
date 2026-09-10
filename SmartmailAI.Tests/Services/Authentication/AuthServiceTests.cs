using System;
using System.Threading.Tasks;
using Moq;
using SmartmailAI.Core.Contracts.Repository;
using SmartmailAI.Core.Contracts.Services.Authentication;
using SmartmailAI.Core.Contracts.Services.LocalSecurity;
using SmartmailAI.Core.Data;
using SmartmailAI.Core.Models;
using SmartmailAI.Core.Services.Authentication;
using Xunit;

namespace SmartmailAI.Tests.Services.Authentication;

public class AuthServiceTests
{
	[Fact]
	public async Task LoginAsync_SetsIsAuthenticated_OnSuccess()
	{
		// Arrange
		var repoMock = new Mock<IAccountRepository>();
		var secretStoreMock = new Mock<IAccountSecretStore>();
		var totpMock = new Mock<ITotpService>();
		var dpapiMock = new Mock<IDpapiService>();

		var password = "StrongPassw0rd!";
		var (hash, salt) = Hasher.HashPassword(password);

		var account = new Account
		{
			IndexGuid = Guid.NewGuid().ToString(),
			Login = "user1",
			PhoneNumber = "0123456789",
			Enabled = true,
			TwoFactorEnabled = false,
			Password = hash,
			Salt = salt
		};

		repoMock.Setup(r => r.GetAccountByLoginAsync("user1")).ReturnsAsync(account);

		var svc = new AuthService(repoMock.Object, secretStoreMock.Object, totpMock.Object, dpapiMock.Object);

		// Act
		var (success, specific) = await svc.LoginAsync("user1", password);

		// Assert
		Assert.True(success);
		Assert.Null(specific);
		Assert.True(svc.IsAuthenticated);
		Assert.Equal("user1", svc.CurrentAccountLogin);
	}

	[Fact]
	public async Task LoginAsync_ReturnsNeedTwoFactor_WhenTwoFactorEnabled()
	{
		// Arrange
		var repoMock = new Mock<IAccountRepository>();
		var secretStoreMock = new Mock<IAccountSecretStore>();
		var totpMock = new Mock<ITotpService>();
		var dpapiMock = new Mock<IDpapiService>();

		var password = "StrongPassw0rd!";
		var (hash, salt) = Hasher.HashPassword(password);

		var account = new Account
		{
			IndexGuid = Guid.NewGuid().ToString(),
			Login = "user2",
			PhoneNumber = "0123456789",
			Enabled = true,
			TwoFactorEnabled = true,
			Password = hash,
			Salt = salt
		};

		repoMock.Setup(r => r.GetAccountByLoginAsync("user2")).ReturnsAsync(account);

		var svc = new AuthService(repoMock.Object, secretStoreMock.Object, totpMock.Object, dpapiMock.Object);

		// Act
		var (success, specific) = await svc.LoginAsync("user2", password);

		// Assert
		Assert.False(success);
		Assert.Equal("Need_TwoFactor", specific);
		Assert.False(svc.IsAuthenticated);
	}

	[Fact]
	public async Task LoginAsync_ReturnsErrorAccountDisabled_WhenAccountNotEnabled()
	{
		// Arrange
		var repoMock = new Mock<IAccountRepository>();
		var secretStoreMock = new Mock<IAccountSecretStore>();
		var totpMock = new Mock<ITotpService>();
		var dpapiMock = new Mock<IDpapiService>();

		var password = "StrongPassw0rd!";
		var (hash, salt) = Hasher.HashPassword(password);

		var account = new Account
		{
			IndexGuid = Guid.NewGuid().ToString(),
			Login = "user3",
			PhoneNumber = "0123456789",
			Enabled = false,
			TwoFactorEnabled = false,
			Password = hash,
			Salt = salt
		};

		repoMock.Setup(r => r.GetAccountByLoginAsync("user3")).ReturnsAsync(account);

		var svc = new AuthService(repoMock.Object, secretStoreMock.Object, totpMock.Object, dpapiMock.Object);

		// Act
		var (success, specific) = await svc.LoginAsync("user3", password);

		// Assert
		Assert.False(success);
		Assert.Equal("Error_AccountDisabled", specific);
		Assert.False(svc.IsAuthenticated);
	}

	[Fact]
	public async Task LoginAsync_ReturnsFalse_OnInvalidPassword()
	{
		// Arrange
		var repoMock = new Mock<IAccountRepository>();
		var secretStoreMock = new Mock<IAccountSecretStore>();
		var totpMock = new Mock<ITotpService>();
		var dpapiMock = new Mock<IDpapiService>();

		var (hash, salt) = Hasher.HashPassword("CorrectPassword123!");

		var account = new Account
		{
			IndexGuid = Guid.NewGuid().ToString(),
			Login = "user4",
			PhoneNumber = "0123456789",
			Enabled = true,
			TwoFactorEnabled = false,
			Password = hash,
			Salt = salt
		};

		repoMock.Setup(r => r.GetAccountByLoginAsync("user4")).ReturnsAsync(account);

		var svc = new AuthService(repoMock.Object, secretStoreMock.Object, totpMock.Object, dpapiMock.Object);

		// Act
		var (success, specific) = await svc.LoginAsync("user4", "WrongPassword!");

		// Assert
		Assert.False(success);
		Assert.Null(specific);
		Assert.False(svc.IsAuthenticated);
	}

	[Fact]
	public async Task RegisterAsync_AddsAccount_WhenValid()
	{
		// Arrange
		var repoMock = new Mock<IAccountRepository>();
		var secretStoreMock = new Mock<IAccountSecretStore>();
		var totpMock = new Mock<ITotpService>();
		var dpapiMock = new Mock<IDpapiService>();

		repoMock.Setup(r => r.LoginExistsAsync("newuser")).ReturnsAsync(false);

		var svc = new AuthService(repoMock.Object, secretStoreMock.Object, totpMock.Object, dpapiMock.Object);

		// Act
		var (success, error) = await svc.RegisterAsync("newuser", "0123456789", "StrongPassw0rd!");

		// Assert
		Assert.True(success);
		Assert.Equal(string.Empty, error);
		repoMock.Verify(r => r.AddAccountAsync(It.Is<Account>(a => a.Login == "newuser" && a.PhoneNumber == "0123456789")),
			Times.Once);
	}

	[Fact]
	public async Task RegisterAsync_ReturnsError_WhenLoginExists()
	{
		// Arrange
		var repoMock = new Mock<IAccountRepository>();
		var secretStoreMock = new Mock<IAccountSecretStore>();
		var totpMock = new Mock<ITotpService>();
		var dpapiMock = new Mock<IDpapiService>();

		repoMock.Setup(r => r.LoginExistsAsync("existing")).ReturnsAsync(true);

		var svc = new AuthService(repoMock.Object, secretStoreMock.Object, totpMock.Object, dpapiMock.Object);

		// Act
		var (success, error) = await svc.RegisterAsync("existing", "0123456789", "StrongPassw0rd!");

		// Assert
		Assert.False(success);
		Assert.Equal("Ce login est déjà utilisé.", error);
	}

	[Fact]
	public async Task Update_EnableDisable_TwoFactorAsync_Enable_CallsSaveSecret()
	{
		// Arrange
		var repoMock = new Mock<IAccountRepository>();
		var secretStoreMock = new Mock<IAccountSecretStore>();
		var totpMock = new Mock<ITotpService>();
		var dpapiMock = new Mock<IDpapiService>();

		var password = "StrongPassw0rd!";
		var (hash, salt) = Hasher.HashPassword(password);

		var account = new Account
		{
			IndexGuid = Guid.NewGuid().ToString(),
			Login = "u5",
			PhoneNumber = "0123456789",
			Enabled = true,
			TwoFactorEnabled = false,
			Password = hash,
			Salt = salt
		};
		repoMock.Setup(r => r.GetAccountByLoginAsync("u5")).ReturnsAsync(account);

		totpMock.Setup(t => t.GenerateSecret()).Returns(new TotpSecret("BASE32SECRET"));
		dpapiMock.Setup(d => d.Encrypt("BASE32SECRET")).Returns("ENCRYPTED");

		var svc = new AuthService(repoMock.Object, secretStoreMock.Object, totpMock.Object, dpapiMock.Object);

		// Act
		await svc.Update_EnableDisable_TwoFactorAsync("u5", true);

		// Assert
		secretStoreMock.Verify(s => s.SaveSecretAsync("u5", "ENCRYPTED"), Times.Once);
	}

	[Fact]
	public async Task Update_EnableDisable_TwoFactorAsync_Disable_CallsDeleteSecret()
	{
		// Arrange
		var repoMock = new Mock<IAccountRepository>();
		var secretStoreMock = new Mock<IAccountSecretStore>();
		var totpMock = new Mock<ITotpService>();
		var dpapiMock = new Mock<IDpapiService>();

		var password = "StrongPassw0rd!";
		var (hash, salt) = Hasher.HashPassword(password);

		var account = new Account
		{
			IndexGuid = Guid.NewGuid().ToString(),
			Login = "u6",
			PhoneNumber = "0123456789",
			Enabled = true,
			TwoFactorEnabled = true,
			Password = hash,
			Salt = salt
		};
		repoMock.Setup(r => r.GetAccountByLoginAsync("u6")).ReturnsAsync(account);

		var svc = new AuthService(repoMock.Object, secretStoreMock.Object, totpMock.Object, dpapiMock.Object);

		// Act
		await svc.Update_EnableDisable_TwoFactorAsync("u6", false);

		// Assert
		secretStoreMock.Verify(s => s.DeleteSecretAsync("u6"), Times.Once);
	}

	[Fact]
	public async Task ValidateSecondFactorAsync_SetsIsAuthenticated_OnValidCode()
	{
		// Arrange
		var repoMock = new Mock<IAccountRepository>();
		var secretStoreMock = new Mock<IAccountSecretStore>();
		var totpMock = new Mock<ITotpService>();
		var dpapiMock = new Mock<IDpapiService>();

		var password = "StrongPassw0rd!";
		var (hash, salt) = Hasher.HashPassword(password);

		var account = new Account
		{
			IndexGuid = Guid.NewGuid().ToString(),
			Login = "u7",
			PhoneNumber = "0123456789",
			Enabled = true,
			TwoFactorEnabled = true,
			Password = hash,
			Salt = salt
		};
		repoMock.Setup(r => r.GetAccountByLoginAsync("u7")).ReturnsAsync(account);
		secretStoreMock.Setup(s => s.GetSecretAsync("u7")).ReturnsAsync("ENCRYPTED");
		dpapiMock.Setup(d => d.Decrypt("ENCRYPTED")).Returns("BASE32");
		totpMock.Setup(t => t.ValidateCode("BASE32", "123456")).Returns(true);

		var svc = new AuthService(repoMock.Object, secretStoreMock.Object, totpMock.Object, dpapiMock.Object);

		// Act
		var ok = await svc.ValidateSecondFactorAsync("u7", "123456");

		// Assert
		Assert.True(ok);
		Assert.True(svc.IsAuthenticated);
	}

	[Fact]
	public async Task ValidateSecondFactorAsync_ReturnsFalse_OnInvalidCode()
	{
		// Arrange
		var repoMock = new Mock<IAccountRepository>();
		var secretStoreMock = new Mock<IAccountSecretStore>();
		var totpMock = new Mock<ITotpService>();
		var dpapiMock = new Mock<IDpapiService>();

		var password = "StrongPassw0rd!";
		var (hash, salt) = Hasher.HashPassword(password);

		var account = new Account
		{
			IndexGuid = Guid.NewGuid().ToString(),
			Login = "u8",
			PhoneNumber = "0123456789",
			Enabled = true,
			TwoFactorEnabled = true,
			Password = hash,
			Salt = salt
		};
		repoMock.Setup(r => r.GetAccountByLoginAsync("u8")).ReturnsAsync(account);
		secretStoreMock.Setup(s => s.GetSecretAsync("u8")).ReturnsAsync("ENCRYPTED");
		dpapiMock.Setup(d => d.Decrypt("ENCRYPTED")).Returns("BASE32");
		totpMock.Setup(t => t.ValidateCode("BASE32", "000000")).Returns(false);

		var svc = new AuthService(repoMock.Object, secretStoreMock.Object, totpMock.Object, dpapiMock.Object);

		// Act
		var ok = await svc.ValidateSecondFactorAsync("u8", "000000");

		// Assert
		Assert.False(ok);
		Assert.False(svc.IsAuthenticated);
	}

	[Fact]
	public async Task UpdateLastConnection_CallsUpdateAccountAsync_WhenAccountExists()
	{
		// Arrange
		var repoMock = new Mock<IAccountRepository>();
		var secretStoreMock = new Mock<IAccountSecretStore>();
		var totpMock = new Mock<ITotpService>();
		var dpapiMock = new Mock<IDpapiService>();

		var password = "StrongPassw0rd!";
		var (hash, salt) = Hasher.HashPassword(password);

		var account = new Account
		{
			IndexGuid = "idx",
			Id = 10,
			Login = "u9",
			PhoneNumber = "0123456789",
			Enabled = true,
			TwoFactorEnabled = true,
			Password = hash,
			Salt = salt
		};
		repoMock.Setup(r => r.GetAccountByLoginAsync("u9")).ReturnsAsync(account);

		var svc = new AuthService(repoMock.Object, secretStoreMock.Object, totpMock.Object, dpapiMock.Object)
		{
			CurrentAccountLogin = "u9"
		};

		// Act
		await svc.UpdateLastConnection();

		// Assert
		repoMock.Verify(r => r.UpdateAccountAsync(It.Is<Account>(a => a.Id == 10 && a.IndexGuid == "idx")), Times.Once);
	}
}
