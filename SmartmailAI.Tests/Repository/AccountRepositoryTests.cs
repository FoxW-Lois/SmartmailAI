using Microsoft.EntityFrameworkCore;
using Moq;
using SmartmailAI.Core.AppDbContext;
using SmartmailAI.Core.Contracts.Services.LocalSecurity;
using SmartmailAI.Core.Models;
using SmartmailAI.Core.Repository;

namespace SmartmailAI.Tests.Repository;

public class AccountRepositoryTests
{
	private static DbContextOptions<AppDbContext_Account> CreateOptions(string dbName)
		=> new DbContextOptionsBuilder<AppDbContext_Account>().UseInMemoryDatabase(dbName).Options;

	private class TestDbContextFactory(DbContextOptions<AppDbContext_Account> options) : IDbContextFactory<AppDbContext_Account>
	{
		private readonly DbContextOptions<AppDbContext_Account> _options = options;

		public AppDbContext_Account CreateDbContext()
		{
			var ctx = new AppDbContext_Account(_options) { Account = null! };
			ctx.Account = ctx.Set<Account>();
			return ctx;
		}
	}

	[Fact]
	public async Task GetAccountByLoginAsync_ReturnsDecryptedAccount()
	{
		// Arrange
		var options = CreateOptions("GetAccountByLogin");

		// Seed data
		using (var ctx = new AppDbContext_Account(options) { Account = null! })
		{
			ctx.Account = ctx.Set<Account>();
			ctx.Account.Add(new Account
			{
				Id = 1,
				Login = "user1",
				IndexGuid = "enc-idx",
				PhoneNumber = "enc-phone",
				Password = "pwd",
				Salt = "salt",
				TwoFactorEnabled = false,
				Enabled = true
			});
			await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		var aesMock = new Mock<IAesService>();
		aesMock.Setup(a => a.DecryptAsync("enc-idx")).ReturnsAsync("dec-idx");
		aesMock.Setup(a => a.DecryptAsync("enc-phone")).ReturnsAsync("dec-phone");

		var factory = new TestDbContextFactory(options);
		var repo = new AccountRepository(factory, aesMock.Object);

		// Act
		var result = await repo.GetAccountByLoginAsync("user1");

		// Assert
		Assert.NotNull(result);
		Assert.Equal("dec-idx", result!.IndexGuid);
		Assert.Equal("dec-phone", result.PhoneNumber);
	}

	[Fact]
	public async Task LoginExistsAsync_ReturnsTrueWhenExists()
	{
		// Arrange
		var options = CreateOptions("LoginExists");

		using (var ctx = new AppDbContext_Account(options) { Account = null! })
		{
			ctx.Account = ctx.Set<Account>();
			ctx.Account.Add(new Account { Id = 1, Login = "exists", IndexGuid = "i", PhoneNumber = "p", Password = "pwd", Salt = "s", TwoFactorEnabled = false, Enabled = true });
			await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		var aesMock = new Mock<IAesService>();
		var factory = new TestDbContextFactory(options);
		var repo = new AccountRepository(factory, aesMock.Object);

		// Act
		var exists = await repo.LoginExistsAsync("exists");

		// Assert
		Assert.True(exists);
	}

	[Fact]
	public async Task AddAccountAsync_EncryptsBeforeSaving()
	{
		// Arrange
		var options = CreateOptions("AddAccount");

		var aesMock = new Mock<IAesService>();
		aesMock.Setup(a => a.EncryptAsync("plain-idx")).ReturnsAsync("enc-idx");
		aesMock.Setup(a => a.EncryptAsync("plain-phone")).ReturnsAsync("enc-phone");

		var factory = new TestDbContextFactory(options);
		var repo = new AccountRepository(factory, aesMock.Object);

		var account = new Account
		{
			Login = "new",
			IndexGuid = "plain-idx",
			PhoneNumber = "plain-phone",
			Password = "pwd",
			Salt = "s",
			TwoFactorEnabled = false,
			Enabled = true
		};

		// Act
		await repo.AddAccountAsync(account);

		// Assert
		using var verifyCtx = new AppDbContext_Account(options) { Account = null! };
		verifyCtx.Account = verifyCtx.Set<Account>();
		var saved = await verifyCtx.Account.FirstOrDefaultAsync(a => a.Login == "new", TestContext.Current.CancellationToken);

		Assert.NotNull(saved);
		Assert.Equal("enc-idx", saved!.IndexGuid);
		Assert.Equal("enc-phone", saved.PhoneNumber);
	}

	[Fact]
	public async Task UpdateAccountAsync_EncryptsBeforeUpdating()
	{
		// Arrange
		var options = CreateOptions("UpdateAccount");

		using (var ctx = new AppDbContext_Account(options) { Account = null! })
		{
			ctx.Account = ctx.Set<Account>();
			ctx.Account.Add(new Account
			{
				Id = 10,
				Login = "toUpdate",
				IndexGuid = "old-idx",
				PhoneNumber = "old-phone",
				Password = "pwd",
				Salt = "s",
				TwoFactorEnabled = false,
				Enabled = true
			});
			await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		var aesMock = new Mock<IAesService>();
		aesMock.Setup(a => a.EncryptAsync("new-idx")).ReturnsAsync("enc-new-idx");
		aesMock.Setup(a => a.EncryptAsync("new-phone")).ReturnsAsync("enc-new-phone");

		var factory = new TestDbContextFactory(options);
		var repo = new AccountRepository(factory, aesMock.Object);

		// Prepare account with same Id but new plain values
		var updated = new Account
		{
			Id = 10,
			Login = "toUpdate",
			IndexGuid = "new-idx",
			PhoneNumber = "new-phone",
			Password = "pwd",
			Salt = "s",
			TwoFactorEnabled = false,
			Enabled = true
		};

		// Act
		await repo.UpdateAccountAsync(updated);

		// Assert
		using var verifyCtx = new AppDbContext_Account(options) { Account = null! };
		verifyCtx.Account = verifyCtx.Set<Account>();
		var saved = await verifyCtx.Account.FirstOrDefaultAsync(a => a.Id == 10, TestContext.Current.CancellationToken);

		Assert.NotNull(saved);
		Assert.Equal("enc-new-idx", saved!.IndexGuid);
		Assert.Equal("enc-new-phone", saved.PhoneNumber);
	}

	[Fact]
	public async Task DeleteAccountAsync_RemovesRecord()
	{
		// Arrange
		var options = CreateOptions("DeleteAccount");

		using (var ctx = new AppDbContext_Account(options) { Account = null! })
		{
			ctx.Account = ctx.Set<Account>();
			ctx.Account.Add(new Account
			{
				Id = 20,
				Login = "todelete",
				IndexGuid = "i",
				PhoneNumber = "p",
				Password = "pwd",
				Salt = "s",
				TwoFactorEnabled = false,
				Enabled = true
			});
			await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		var aesMock = new Mock<IAesService>();
		aesMock.Setup(a => a.EncryptAsync(It.IsAny<string>())).ReturnsAsync((string s) => s);

		var factory = new TestDbContextFactory(options);
		var repo = new AccountRepository(factory, aesMock.Object);

		// Act
		var toDelete = new Account
		{
			Id = 20,
			Login = "todelete",
			IndexGuid = "i",
			PhoneNumber = "p",
			Password = "pwd",
			Salt = "s",
			TwoFactorEnabled = false,
			Enabled = true
		};
		await repo.DeleteAccountAsync(toDelete);

		// Assert
		using var verifyCtx = new AppDbContext_Account(options) { Account = null! };
		verifyCtx.Account = verifyCtx.Set<Account>();
		var exists = await verifyCtx.Account.AnyAsync(a => a.Id == 20, TestContext.Current.CancellationToken);

		Assert.False(exists);
	}
}
