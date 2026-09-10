using Moq;
using SmartmailAI.Core.Contracts.Repository;
using SmartmailAI.Core.Contracts.Services.Addresses;
using SmartmailAI.Core.Contracts.Services.Authentication;
using SmartmailAI.Core.Models;
using SmartmailAI.Core.Services;

namespace SmartmailAI.Tests.Services;

public class EmailsSyncServiceTests
{
	[Fact]
	public async Task SyncNewEmailsAsync_AddsReturnedEmailsToRepository()
	{
		// Arrange
		var mailReaderMock = new Mock<IMailReaderService>();
		var emailRepoMock = new Mock<IEmailRepository>();
		var addressesRepoMock = new Mock<IAddressesRepository>();
		var authServiceMock = new Mock<IAuthService>();

		var account = new AccountMailBase
		{
			IndexGuidHash = "hash",
			Email = "user@example.com",
			ConnectedAt = DateTime.UtcNow,
			TokenStorageKey = "tokenKey"
		};

		var emails = new List<Email>
		{
			new(),
			new()
		};

		mailReaderMock
			.Setup(m => m.GetLastMessagesFromAccountAsync(false, account))
			.ReturnsAsync(emails);

		emailRepoMock.Setup(r => r.AddEmailAsync(It.IsAny<Email>())).Returns(Task.CompletedTask);

		var service = new EmailsSyncService(mailReaderMock.Object, emailRepoMock.Object, addressesRepoMock.Object,
			authServiceMock.Object);

		// Act
		await service.SyncNewEmailsAsync(account);

		// Assert
		emailRepoMock.Verify(r => r.AddEmailAsync(It.IsAny<Email>()), Times.Exactly(emails.Count));
	}

	[Fact]
	public async Task SyncNewEmailsAsync_WhenNoEmailsReturned_DoesNotCallRepository()
	{
		// Arrange
		var mailReaderMock = new Mock<IMailReaderService>();
		var emailRepoMock = new Mock<IEmailRepository>();
		var addressesRepoMock = new Mock<IAddressesRepository>();
		var authServiceMock = new Mock<IAuthService>();

		var account = new AccountMailBase
		{
			IndexGuidHash = "hash",
			Email = "user@example.com",
			ConnectedAt = DateTime.UtcNow,
			TokenStorageKey = "tokenKey"
		};

		mailReaderMock
			.Setup(m => m.GetLastMessagesFromAccountAsync(false, account))
			.ReturnsAsync((List<Email>?)null);

		var service = new EmailsSyncService(mailReaderMock.Object, emailRepoMock.Object, addressesRepoMock.Object,
			authServiceMock.Object);

		// Act
		await service.SyncNewEmailsAsync(account);

		// Assert
		emailRepoMock.Verify(r => r.AddEmailAsync(It.IsAny<Email>()), Times.Never);
	}
}
