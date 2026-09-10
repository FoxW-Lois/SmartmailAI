using Moq;
using SmartmailAI.Core.Contracts.Repository;
using SmartmailAI.Core.Contracts.Services.Security;
using SmartmailAI.Core.Models;
using SmartmailAI.Core.Services;

namespace SmartmailAI.Tests.Services;

public class EmailsServiceTests
{
	[Fact]
	public async Task MarkEmailAsStarredAsync_UpdatesRepository_WhenNotHardcoded()
	{
		// Arrange
		var emailRepo = new Mock<IEmailRepository>();
		var svc = new EmailsService(emailRepo.Object, Mock.Of<IRedFlagDomainService>(), Mock.Of<IVirusTotalService>(),
			Mock.Of<IDnsSecurityService>(), Mock.Of<IMLDA_Repository>());

		var email = new Email { Guid = "g-1", IsStarred = false };

		// Act
		await svc.MarkEmailAsStarredAsync(email);

		// Assert
		Assert.True(email.IsStarred);
		emailRepo.Verify(e => e.UpdateEmailAsync(It.Is<Email>(x => x.Guid == "g-1")), Times.Once);
	}

	[Fact]
	public async Task MarkEmailAsStarredAsync_DoesNotCallRepository_WhenHardcoded()
	{
		// Arrange
		var emailRepo = new Mock<IEmailRepository>();
		var svc = new EmailsService(emailRepo.Object, Mock.Of<IRedFlagDomainService>(), Mock.Of<IVirusTotalService>(),
			Mock.Of<IDnsSecurityService>(), Mock.Of<IMLDA_Repository>());

		var email = new Email { Guid = "Email_Hardcoded-123", IsStarred = false };

		// Act
		await svc.MarkEmailAsStarredAsync(email);

		// Assert
		Assert.True(email.IsStarred);
		emailRepo.Verify(e => e.UpdateEmailAsync(It.IsAny<Email>()), Times.Never);
	}

	[Fact]
	public async Task MarkEmailAsReadAsync_UpdatesRepository_WhenNotHardcoded()
	{
		// Arrange
		var emailRepo = new Mock<IEmailRepository>();
		var svc = new EmailsService(emailRepo.Object, Mock.Of<IRedFlagDomainService>(), Mock.Of<IVirusTotalService>(),
			Mock.Of<IDnsSecurityService>(), Mock.Of<IMLDA_Repository>());

		var email = new Email { Guid = "g-2", IsRead = false };

		// Act
		await svc.MarkEmailAsReadAsync(email);

		// Assert
		Assert.True(email.IsRead);
		emailRepo.Verify(e => e.UpdateEmailAsync(It.Is<Email>(x => x.Guid == "g-2")), Times.Once);
	}

	[Fact]
	public async Task MarkEmailAsTrashedAsync_SetsPreviousAndCallsUpdate()
	{
		// Arrange
		var emailRepo = new Mock<IEmailRepository>();
		var svc = new EmailsService(emailRepo.Object, Mock.Of<IRedFlagDomainService>(), Mock.Of<IVirusTotalService>(),
			Mock.Of<IDnsSecurityService>(), Mock.Of<IMLDA_Repository>());

		var email = new Email { Guid = "g-3", MailboxType = MailboxType.Inbox };

		// Act
		await svc.MarkEmailAsTrashedAsync(email);

		// Assert
		Assert.Equal(MailboxType.Trash, email.MailboxType);
		Assert.Equal(MailboxType.Inbox, email.PreviousMailboxType);
		emailRepo.Verify(e => e.UpdateEmailAsync(It.Is<Email>(x => x.Guid == "g-3")), Times.Once);
	}

	[Fact]
	public async Task ApplySecurityAnalysisAsync_SkipsForDrafts_NoDependencyCalls()
	{
		// Arrange
		var redFlag = new Mock<IRedFlagDomainService>();
		var virus = new Mock<IVirusTotalService>();
		var dns = new Mock<IDnsSecurityService>();
		var mlda = new Mock<IMLDA_Repository>();

		var svc = new EmailsService(Mock.Of<IEmailRepository>(), redFlag.Object, virus.Object, dns.Object, mlda.Object);

		var email = new Email { Guid = "g-4", MailboxType = MailboxType.Drafts, Content = "http://bit.ly/1" };

		// Act
		await svc.ApplySecurityAnalysisAsync(email);

		// Assert
		// For drafts, ApplySecurityAnalysisAsync should return early and not call services
		redFlag.Verify(r => r.IsFlaggedDomainAsync(It.IsAny<string>()), Times.Never);
		virus.Verify(v => v.AnalyzeAttachmentsAsync(It.IsAny<List<MailAttachment>>()), Times.Never);
		dns.Verify(d => d.CheckDomainAsync(It.IsAny<string>()), Times.Never);
	}

	[Fact]
	public async Task GetAllCategoriesAsync_ReturnsExpectedMailboxTypes()
	{
		// Arrange
		var svc = new EmailsService(Mock.Of<IEmailRepository>(), Mock.Of<IRedFlagDomainService>(), Mock.Of<IVirusTotalService>(),
			Mock.Of<IDnsSecurityService>(), Mock.Of<IMLDA_Repository>());

		// Act
		var cats = await svc.GetAllCategoriesAsync();

		// Assert
		var types = new HashSet<MailboxType>(cats.Select(c => c.MailboxType));
		Assert.Contains(MailboxType.Drafts, types);
		Assert.Contains(MailboxType.Starred, types);
		Assert.Contains(MailboxType.Unread, types);
		Assert.Contains(MailboxType.Trash, types);
		Assert.Contains(MailboxType.AllMails, types);
		Assert.Contains(MailboxType.Archives, types);
		Assert.Contains(MailboxType.PhishingSpam, types);
	}

	[Fact]
	public async Task GetMailboxEmailsAsync_UsesRepositoryAndReturnsListAndCount()
	{
		// Arrange
		var repo = new Mock<IEmailRepository>();
		var sample = new List<Email> { new Email { Guid = "e1" } };
		repo.Setup(r => r.GetEmailsByAddressAndMailboxTypeAsync(MailboxType.Inbox, "owner", 1, 10))
			.ReturnsAsync((sample, 1));

		var svc = new EmailsService(repo.Object, Mock.Of<IRedFlagDomainService>(), Mock.Of<IVirusTotalService>(),
			Mock.Of<IDnsSecurityService>(), Mock.Of<IMLDA_Repository>());

		// Act
		var (list, count) = await svc.GetMailboxEmailsAsync(MailboxType.Inbox, "owner", 1, 10);

		// Assert
		Assert.Equal(1, count);
		var asList = list.ToList();
		Assert.Single(asList);
		Assert.Equal("e1", asList[0].Guid);
	}

	[Fact]
	public async Task ScribbleEmailAsync_CallsAddWhenGuidNull_AndUpdateWhenGuidProvided()
	{
		// Arrange
		var repo = new Mock<IEmailRepository>();
		repo.Setup(r => r.AddEmailAsync(It.IsAny<Email>())).Returns(Task.CompletedTask);
		repo.Setup(r => r.UpdateEmailAsync(It.IsAny<Email>())).Returns(Task.CompletedTask);

		var svc = new EmailsService(repo.Object, Mock.Of<IRedFlagDomainService>(), Mock.Of<IVirusTotalService>(),
			Mock.Of<IDnsSecurityService>(), Mock.Of<IMLDA_Repository>());

		// Act - add
		await svc.ScribbleEmailAsync(null, "from@x.com", "to@x.com", "s", "b", null, null);

		// Assert
		repo.Verify(r => r.AddEmailAsync(It.IsAny<Email>()), Times.Once);

		// Act - update
		await svc.ScribbleEmailAsync("guid-1", "from@x.com", "to@x.com", "s", "b", null, null);

		// Assert
		repo.Verify(r => r.UpdateEmailAsync(It.IsAny<Email>()), Times.Once);
	}

	[Fact]
	public async Task MarkEmailAsUnreadAsync_UpdatesRepository_WhenNotHardcoded()
	{
		// Arrange
		var emailRepo = new Mock<IEmailRepository>();
		var svc = new EmailsService(emailRepo.Object, Mock.Of<IRedFlagDomainService>(), Mock.Of<IVirusTotalService>(),
			Mock.Of<IDnsSecurityService>(), Mock.Of<IMLDA_Repository>());

		var email = new Email { Guid = "g-u", IsRead = true };

		// Act
		await svc.MarkEmailAsUnreadAsync(email);

		// Assert
		Assert.False(email.IsRead);
		emailRepo.Verify(e => e.UpdateEmailAsync(It.Is<Email>(x => x.Guid == "g-u")), Times.Once);
	}

	[Fact]
	public async Task MarkEmailAsArchivedAsync_AndRestoreEmailAsync_CallRepository_WhenNotHardcoded()
	{
		// Arrange
		var emailRepo = new Mock<IEmailRepository>();
		var svc = new EmailsService(emailRepo.Object, Mock.Of<IRedFlagDomainService>(), Mock.Of<IVirusTotalService>(),
			Mock.Of<IDnsSecurityService>(), Mock.Of<IMLDA_Repository>());

		var email = new Email { Guid = "g-a", MailboxType = MailboxType.Inbox };
		// Act
		await svc.MarkEmailAsArchivedAsync(email);

		// Assert - at least repository updated and previous mailbox recorded
		emailRepo.Verify(e => e.UpdateEmailAsync(It.Is<Email>(x => x.Guid == "g-a")), Times.Once);
		Assert.True(email.PreviousMailboxType == MailboxType.Inbox || email.MailboxType == MailboxType.Archives);

		// Act - restore
		await svc.RestoreEmailAsync(email);

		// Assert - repository updated on restore as well
		emailRepo.Verify(e => e.UpdateEmailAsync(It.IsAny<Email>()), Times.AtLeast(2));
	}

	[Fact]
	public async Task DeleteEmailAsync_RemovesFromAllEmailsAndCallsRepository()
	{
		// Arrange
		var repo = new Mock<IEmailRepository>();
		repo.Setup(r => r.GetEmailsByAddressAndMailboxTypeAsync(MailboxType.Inbox, "owner", 1, 10))
			.ReturnsAsync((new List<Email> { new() { Guid = "del-1", Owner = "o" } }, 1));
		repo.Setup(r => r.DeleteEmailAsync(It.IsAny<Email>())).Returns(Task.CompletedTask);

		var svc = new EmailsService(repo.Object, Mock.Of<IRedFlagDomainService>(), Mock.Of<IVirusTotalService>(),
			Mock.Of<IDnsSecurityService>(), Mock.Of<IMLDA_Repository>());

		var (list, _) = await svc.GetMailboxEmailsAsync(MailboxType.Inbox, "owner", 1, 10);
		var asList = list.ToList();
		var email = asList[0];

		// Act
		await svc.DeleteEmailAsync(email);

		// Assert - repository called
		repo.Verify(r => r.DeleteEmailAsync(It.Is<Email>(e => e.Guid == "del-1")), Times.Once);

		// Re-query mailbox emails to observe updated collection
		var (listAfter, _) = await svc.GetMailboxEmailsAsync(MailboxType.Inbox, "owner", 1, 10);
		var asListAfter = listAfter.ToList();
		Assert.DoesNotContain(asListAfter, e => e.Guid == "del-1");
	}

	[Fact]
	public async Task MarkEmailAsPhishingSpamAsync_And_NotPhishingSpamAsync_CallRepository()
	{
		// Arrange
		var emailRepo = new Mock<IEmailRepository>();
		var svc = new EmailsService(emailRepo.Object, Mock.Of<IRedFlagDomainService>(), Mock.Of<IVirusTotalService>(),
			Mock.Of<IDnsSecurityService>(), Mock.Of<IMLDA_Repository>());

		var email = new Email { Guid = "g-p", MailboxType = MailboxType.Inbox };

		// Act
		await svc.MarkEmailAsPhishingSpamAsync(email);

		// Assert
		Assert.Equal(MailboxType.PhishingSpam, email.MailboxType);
		emailRepo.Verify(e => e.UpdateEmailAsync(It.Is<Email>(x => x.Guid == "g-p")), Times.Once);

		// Act - not phishing
		await svc.MarkEmailAsNotPhishingSpamAsync(email);

		// Assert
		Assert.Equal(MailboxType.Inbox, email.MailboxType);
		emailRepo.Verify(e => e.UpdateEmailAsync(It.IsAny<Email>()), Times.AtLeast(2));
	}
}
