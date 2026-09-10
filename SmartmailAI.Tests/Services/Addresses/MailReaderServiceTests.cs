using Google.Apis.Auth.OAuth2;
using Moq;
using SmartmailAI.Core.Contracts.Repository;
using SmartmailAI.Core.Contracts.Services;
using SmartmailAI.Core.Contracts.Services.Addresses;
using SmartmailAI.Core.Contracts.Services.Authentication;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Tests.Services.Addresses;

public class MailReaderServiceTests
{
	[Fact]
	public async Task GetLastMessagesFromAccountAsync_ReturnsEmpty_WhenAccountIsNull()
	{
		// Arrange
		var emailRepo = new Mock<IEmailRepository>();
		var gmailCred = new Mock<IGmailCredentialService>();
		var gmailApi = new Mock<IGmailApiService>();
		var otherCred = new Mock<IOtherCredentialService>();
		var otherProtocol = new Mock<IOtherProtocolService>();
		var otherToken = new Mock<IOtherTokenStore>();
		var auth = new Mock<IAuthService>();
		var accountRepo = new Mock<IAccountRepository>();
		var accountService = new Mock<IAccountService>();
		var addressesRepo = new Mock<IAddressesRepository>();
		var mappers = new Mock<IMappersToEmailDTOService>();

		accountService.Setup(a => a.GetAccountByLoginInLocalSessionAsync()).ReturnsAsync((Account?)null);

		var svc = new SmartmailAI.Core.Services.Addresses.MailReaderService(
			emailRepo.Object, gmailCred.Object, gmailApi.Object, otherCred.Object, otherProtocol.Object,
			otherToken.Object, auth.Object, accountRepo.Object, accountService.Object, addressesRepo.Object, mappers.Object
		);

		var mailAccount = new AccountMailBase
		{
			IndexGuidHash = "hash",
			IsFirstConnection = false,
			Email = "u@t.test",
			ConnectedAt = DateTime.UtcNow,
			TokenStorageKey = "token"
		};

		// Act
		var result = await svc.GetLastMessagesFromAccountAsync(false, mailAccount);

		// Assert
		Assert.NotNull(result);
		Assert.Empty(result);
	}

	[Fact]
	public async Task GetLastMessagesFromAccountAsync_ReturnsEmpty_WhenAccountIsFirstConnection()
	{
		// Arrange
		var emailRepo = new Mock<IEmailRepository>();
		var gmailCred = new Mock<IGmailCredentialService>();
		var gmailApi = new Mock<IGmailApiService>();
		var otherCred = new Mock<IOtherCredentialService>();
		var otherProtocol = new Mock<IOtherProtocolService>();
		var otherToken = new Mock<IOtherTokenStore>();
		var auth = new Mock<IAuthService>();
		var accountRepo = new Mock<IAccountRepository>();
		var accountService = new Mock<IAccountService>();
		var addressesRepo = new Mock<IAddressesRepository>();
		var mappers = new Mock<IMappersToEmailDTOService>();

		var account = new Account
		{
			IndexGuid = "hash",
			Login = "user",
			PhoneNumber = "1234567890",
			Password = "password",
			Salt = "salt",
			TwoFactorEnabled = false,
			Enabled = true,

			IsFirstConnection = true
		};
		accountService.Setup(a => a.GetAccountByLoginInLocalSessionAsync()).ReturnsAsync(account);

		var svc = new SmartmailAI.Core.Services.Addresses.MailReaderService(
			emailRepo.Object, gmailCred.Object, gmailApi.Object, otherCred.Object, otherProtocol.Object,
			otherToken.Object, auth.Object, accountRepo.Object, accountService.Object, addressesRepo.Object, mappers.Object
		);

		var mailAccount = new AccountMailBase
		{
			IndexGuidHash = "hash",
			IsFirstConnection = false,
			Email = "u@t.test",
			ConnectedAt = DateTime.UtcNow,
			TokenStorageKey = "token"
		};

		// Act
		var result = await svc.GetLastMessagesFromAccountAsync(false, mailAccount);

		// Assert
		Assert.NotNull(result);
		Assert.Empty(result);
	}

	[Fact]
	public async Task GetLastMessagesFromAccountAsync_ReturnsEmpty_WhenMailAccountIsNotGmailOrOther()
	{
		// Arrange
		var emailRepo = new Mock<IEmailRepository>();
		var gmailCred = new Mock<IGmailCredentialService>();
		var gmailApi = new Mock<IGmailApiService>();
		var otherCred = new Mock<IOtherCredentialService>();
		var otherProtocol = new Mock<IOtherProtocolService>();
		var otherToken = new Mock<IOtherTokenStore>();
		var auth = new Mock<IAuthService>();
		var accountRepo = new Mock<IAccountRepository>();
		var accountService = new Mock<IAccountService>();
		var addressesRepo = new Mock<IAddressesRepository>();
		var mappers = new Mock<IMappersToEmailDTOService>();

		var account = new Account
		{
			IndexGuid = "hash",
			Login = "user",
			PhoneNumber = "1234567890",
			Password = "password",
			Salt = "salt",
			TwoFactorEnabled = false,
			Enabled = true,

			IsFirstConnection = false,
			NbOpenAppByWeek = 7,
			AverageDailyTrafic = "1 à 30 mails par jour",
			RetrievedAllEmails = false,
			DatePicked = DateOnly.FromDateTime(DateTime.UtcNow)
		};

		accountService.Setup(a => a.GetAccountByLoginInLocalSessionAsync()).ReturnsAsync(account);

		var svc = new SmartmailAI.Core.Services.Addresses.MailReaderService(
			emailRepo.Object, gmailCred.Object, gmailApi.Object, otherCred.Object, otherProtocol.Object,
			otherToken.Object, auth.Object, accountRepo.Object, accountService.Object, addressesRepo.Object, mappers.Object
		);

		var mailAccount = new AccountMailBase
		{
			IndexGuidHash = "hash",
			IsFirstConnection = false,
			Email = "not-a-gmail@test",
			ConnectedAt = DateTime.UtcNow,
			TokenStorageKey = "token"
		};

		// Act
		var result = await svc.GetLastMessagesFromAccountAsync(false, mailAccount);

		// Assert
		Assert.NotNull(result);
		Assert.Empty(result);
	}

	[Fact]
	public async Task GetLastMessagesFromAccountAsync_WithGmailCredentialNull_ReturnsEmpty_AndUpdatesAddressOnFirstConnection()
	{
		// Arrange
		var emailRepo = new Mock<IEmailRepository>();
		var gmailCred = new Mock<IGmailCredentialService>();
		var gmailApi = new Mock<IGmailApiService>();
		var otherCred = new Mock<IOtherCredentialService>();
		var otherProtocol = new Mock<IOtherProtocolService>();
		var otherToken = new Mock<IOtherTokenStore>();
		var auth = new Mock<IAuthService>();
		var accountRepo = new Mock<IAccountRepository>();
		var accountService = new Mock<IAccountService>();
		var addressesRepo = new Mock<IAddressesRepository>();
		var mappers = new Mock<IMappersToEmailDTOService>();

		var account = new Account
		{
			IndexGuid = "hash",
			Login = "user",
			PhoneNumber = "1234567890",
			Password = "password",
			Salt = "salt",
			TwoFactorEnabled = false,
			Enabled = true,

			IsFirstConnection = false,
			NbOpenAppByWeek = 7,
			AverageDailyTrafic = "1 à 30 mails par jour",
			RetrievedAllEmails = false,
			DatePicked = DateOnly.FromDateTime(DateTime.UtcNow)
		};

		accountService.Setup(a => a.GetAccountByLoginInLocalSessionAsync()).ReturnsAsync(account);

		// When credential service returns null, GetGmailMessagesAsync will return an empty list (not null)
		gmailCred.Setup(g => g.GetCredentialAsync(It.IsAny<AccountGmail>(), It.IsAny<bool>()))
			.ReturnsAsync((UserCredential?)null);

		// Map empty list to empty emails
		mappers.Setup(m => m.MapEmailFromAddressToEmail_List(It.IsAny<List<EmailFromAddress>>()))
			.ReturnsAsync([]);

		// KeepOnlyNewEmailsAsync returns empty list
		emailRepo.Setup(e => e.KeepOnlyNewEmailsAsync(It.IsAny<string>(), It.IsAny<List<Email>>(), It.IsAny<bool>()))
			.ReturnsAsync([]);

		var svc = new SmartmailAI.Core.Services.Addresses.MailReaderService(
			emailRepo.Object, gmailCred.Object, gmailApi.Object, otherCred.Object, otherProtocol.Object,
			otherToken.Object, auth.Object, accountRepo.Object, accountService.Object, addressesRepo.Object, mappers.Object
		);

		var mailAccount = new AccountGmail
		{
			IndexGuidHash = "hash",
			IsFirstConnection = true,
			Email = "user@gmail.test",
			ConnectedAt = DateTime.UtcNow,
			TokenStorageKey = "token",
			GoogleUserId = "google-user-id"
		};

		// Act
		var result = await svc.GetLastMessagesFromAccountAsync(false, mailAccount);

		// Assert
		Assert.NotNull(result);
		Assert.Empty(result);
		// Verify that the address update was requested because it was the first connection
		addressesRepo.Verify(a => a.UpdateAddressAsync(It.Is<AccountMailBase>(x => x.Email == "user@gmail.test")), Times.Once);
	}
}
