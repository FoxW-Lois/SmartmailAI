using Microsoft.EntityFrameworkCore;
using Moq;
using SmartmailAI.Core.AppDbContext;
using SmartmailAI.Core.Contracts.Services.LocalSecurity;
using SmartmailAI.Core.Data;
using SmartmailAI.Core.Models;
using SmartmailAI.Core.Repository;

namespace SmartmailAI.Tests.Repository;

public class EmailRepositoryTests
{
	private static DbContextOptions<AppDbContext_Email> CreateOptions(string dbName)
		=> new DbContextOptionsBuilder<AppDbContext_Email>().UseInMemoryDatabase(dbName).Options;

	private class TestDbContextFactory(DbContextOptions<AppDbContext_Email> options) : IDbContextFactory<AppDbContext_Email>
	{
		private readonly DbContextOptions<AppDbContext_Email> _options = options;

		public AppDbContext_Email CreateDbContext()
		{
			var ctx = new AppDbContext_Email(_options) { Email = null! };
			ctx.Email = ctx.Set<Email>();
			return ctx;
		}
	}

	[Fact]
	public async Task GetEmailsByAddressAndMailboxTypeAsync_ReturnsDecryptedAndCount()
	{
		// Arrange
		var options = CreateOptions("GetEmailsByAddress");
		var owner = "owner@example.com";
		var ownerHash = Hasher.HashDataWithoutSalt(owner);

		using (var ctx = new AppDbContext_Email(options) { Email = null! })
		{
			ctx.Email = ctx.Set<Email>();
			ctx.Email.Add(new Email
			{
				Guid = "g1",
				SenderEmail = "enc-sender",
				SenderName = "enc-name",
				Owner = "enc-owner",
				OwnerHash = ownerHash,
				MailboxType = MailboxType.Inbox,
				DateSent = DateTime.UtcNow
			});
			await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		var aesMock = new Mock<IAesService>();
		aesMock.Setup(a => a.DecryptAsync("enc-sender")).ReturnsAsync("sender@example.com");
		aesMock.Setup(a => a.DecryptAsync("enc-name")).ReturnsAsync("Sender Name");
		aesMock.Setup(a => a.DecryptAsync("enc-owner")).ReturnsAsync(owner);

		var factory = new TestDbContextFactory(options);
		var repo = new EmailRepository(factory, aesMock.Object);

		// Act
		var (list, count) = await repo.GetEmailsByAddressAndMailboxTypeAsync(MailboxType.Inbox, owner, 1, 10);

		// Assert
		Assert.Equal(1, count);
		var item = Assert.Single(list);
		Assert.Equal("sender@example.com", item.SenderEmail);
		Assert.Equal(owner, item.Owner);
	}

	[Fact]
	public async Task AddEmailAsync_EncryptsBeforeSaving()
	{
		// Arrange
		var options = CreateOptions("AddEmail");

		var aesMock = new Mock<IAesService>();
		aesMock.Setup(a => a.EncryptAsync("plain-sender")).ReturnsAsync("enc-sender");
		aesMock.Setup(a => a.EncryptAsync("plain-name")).ReturnsAsync("enc-name");
		aesMock.Setup(a => a.EncryptAsync("plain-owner")).ReturnsAsync("enc-owner");

		var factory = new TestDbContextFactory(options);
		var repo = new EmailRepository(factory, aesMock.Object);

		var email = new Email
		{
			Guid = "g-add",
			SenderEmail = "plain-sender",
			SenderName = "plain-name",
			Owner = "plain-owner",
			OwnerHash = Hasher.HashDataWithoutSalt("plain-owner"),
			MailboxType = MailboxType.Inbox,
			DateSent = DateTime.UtcNow
		};

		// Act
		await repo.AddEmailAsync(email);

		// Assert
		using var verifyCtx = new AppDbContext_Email(options) { Email = null! };
		verifyCtx.Email = verifyCtx.Set<Email>();
		var saved = await verifyCtx.Email.FirstOrDefaultAsync(e => e.Guid == "g-add", TestContext.Current.CancellationToken);
		Assert.NotNull(saved);
		Assert.Equal("enc-sender", saved!.SenderEmail);
		Assert.Equal("enc-name", saved.SenderName);
		Assert.Equal("enc-owner", saved.Owner);
	}

	[Fact]
	public void NormalizeGuid_RemovesSuffix()
	{
		// Arrange
		var factory = new TestDbContextFactory(CreateOptions("Norm"));
		var repo = new EmailRepository(factory, Mock.Of<IAesService>());

		// Act
		var normalized = repo.NormalizeGuid("abc-123");
		var unchanged = repo.NormalizeGuid("nodef");

		// Assert
		Assert.Equal("abc", normalized);
		Assert.Equal("nodef", unchanged);
	}

	[Fact]
	public async Task KeepOnlyNewEmailsAsync_FiltersDuplicates_BothModes()
	{
		// Arrange
		var options = CreateOptions("KeepOnlyNew");
		var owner = "owner@x.com";
		var ownerHash = Hasher.HashDataWithoutSalt(owner);

		using (var ctx = new AppDbContext_Email(options) { Email = null! })
		{
			ctx.Email = ctx.Set<Email>();
			ctx.Email.Add(new Email { Guid = "g1", OwnerHash = ownerHash, Owner = "owner-encrypted", SenderEmail = "s1", SenderName = "n1" });
			ctx.Email.Add(new Email { Guid = "g2", OwnerHash = ownerHash, Owner = "owner-encrypted", SenderEmail = "s2", SenderName = "n2" });
			await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		var aesMock = new Mock<IAesService>();
		var factory = new TestDbContextFactory(options);
		var repo = new EmailRepository(factory, aesMock.Object);

		var newEmails = new List<Email>
		{
			new() { Guid = "g1" }, // duplicate
			new() { Guid = "g3" }, // new
			new() { Guid = "g2-1" } // duplicate when normalized
		};

		// Act: not from other address => compare guids directly
		var keep1 = await repo.KeepOnlyNewEmailsAsync(owner, newEmails, false);

		// Act: from other address => compare normalized
		var keep2 = await repo.KeepOnlyNewEmailsAsync(owner, newEmails, true);

		// Assert
		Assert.Equal(2, keep1.Count); // g3 and g2-1 (g1 removed)
		Assert.Single(keep2); // only g3 remains because g2-1 normalized to g2 which exists
		Assert.Equal("g3", keep2[0].Guid);
	}
}
