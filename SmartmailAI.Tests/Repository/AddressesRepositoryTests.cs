using Microsoft.EntityFrameworkCore;
using Moq;
using SmartmailAI.Core.AppDbContext;
using SmartmailAI.Core.Contracts.Services;
using SmartmailAI.Core.Contracts.Services.LocalSecurity;
using SmartmailAI.Core.Data;
using SmartmailAI.Core.Models;
using SmartmailAI.Core.Repository;

namespace SmartmailAI.Tests.Repository;

public class AddressesRepositoryTests
{
	private static DbContextOptions<AppDbContext_Address> CreateOptions(string dbName)
		=> new DbContextOptionsBuilder<AppDbContext_Address>().UseInMemoryDatabase(dbName).Options;

	private class TestDbContextFactory(DbContextOptions<AppDbContext_Address> options) : IDbContextFactory<AppDbContext_Address>
	{
		private readonly DbContextOptions<AppDbContext_Address> _options = options;

		public AppDbContext_Address CreateDbContext()
		{
			var ctx = new AppDbContext_Address(_options)
			{
				AccountGmail = null!,
				AccountOther = null!,
				AccountMailBase = null!
			};
			ctx.AccountGmail = ctx.Set<AccountGmail>();
			ctx.AccountOther = ctx.Set<AccountOther>();
			ctx.AccountMailBase = ctx.Set<AccountMailBase>();
			return ctx;
		}
	}

	[Fact]
	public async Task GetAllAddressesByAccountIndexGuidAsync_ReturnsDecrypted()
	{
		// Arrange
		var options = CreateOptions("GetAllAddresses");

		// Prepare account service
		var account = new Account
		{
			Id = 1,
			Login = "u",
			PhoneNumber = "1234567890",
			IndexGuid = "idx-guid",
			Password = "p",
			Salt = "s",
			TwoFactorEnabled = false,
			Enabled = true
		};

		// Seed addresses with hashed index guid
		var indexHash = Hasher.HashDataWithoutSalt(account.IndexGuid);

		using (var ctx = new AppDbContext_Address(options)
		{
			AccountGmail = null!,
			AccountOther = null!,
			AccountMailBase = null!
		})
		{
			ctx.AccountGmail = ctx.Set<AccountGmail>();
			ctx.AccountOther = ctx.Set<AccountOther>();
			ctx.AccountMailBase = ctx.Set<AccountMailBase>();

			ctx.AccountMailBase.Add(new AccountMailBase
			{
				IndexGuidHash = indexHash,
				Email = "enc-email",
				TokenStorageKey = "enc-token",
				ConnectedAt = DateTime.UtcNow
			});

			await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		var aesMock = new Mock<IAesService>();
		aesMock.Setup(a => a.DecryptAsync("enc-email")).ReturnsAsync("dec@example.com");
		aesMock.Setup(a => a.DecryptAsync("enc-token")).ReturnsAsync("decrypted-token");

		var accountServiceMock = new Mock<IAccountService>();
		accountServiceMock.Setup(a => a.GetAccountByLoginInLocalSessionAsync()).ReturnsAsync(account);

		var factory = new TestDbContextFactory(options);
		var repo = new AddressesRepository(factory, aesMock.Object, accountServiceMock.Object);

		// Act
		var list = await repo.GetAllAddressesByAccountIndexGuidAsync();

		// Assert
		Assert.NotNull(list);
		var item = Assert.Single(list);
		Assert.Equal("dec@example.com", item.Email);
		Assert.Equal("decrypted-token", item.TokenStorageKey);
	}

	[Fact]
	public async Task GetAddressByEmailAsync_FindsByDecryptedEmail()
	{
		// Arrange
		var options = CreateOptions("GetAddressByEmail");

		var indexHash = Hasher.HashDataWithoutSalt("some-index");

		using (var ctx = new AppDbContext_Address(options)
		{
			AccountGmail = null!,
			AccountOther = null!,
			AccountMailBase = null!
		})
		{
			ctx.AccountGmail = ctx.Set<AccountGmail>();
			ctx.AccountOther = ctx.Set<AccountOther>();
			ctx.AccountMailBase = ctx.Set<AccountMailBase>();

			ctx.AccountMailBase.Add(new AccountMailBase
			{
				IndexGuidHash = indexHash,
				Email = "enc-email",
				TokenStorageKey = "enc-token",
				ConnectedAt = DateTime.UtcNow
			});
			await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		var aesMock = new Mock<IAesService>();
		aesMock.Setup(a => a.DecryptAsync("enc-email")).ReturnsAsync("user@example.com");
		aesMock.Setup(a => a.DecryptAsync("enc-token")).ReturnsAsync("tok");

		var accountServiceMock = new Mock<IAccountService>();
		accountServiceMock.Setup(a => a.GetAccountByLoginInLocalSessionAsync()).ReturnsAsync((Account?)null);

		var factory = new TestDbContextFactory(options);
		var repo = new AddressesRepository(factory, aesMock.Object, accountServiceMock.Object);

		// Act
		var found = await repo.GetAddressByEmailAsync("user@example.com");

		// Assert
		Assert.NotNull(found);
		Assert.Equal("user@example.com", found!.Email);
	}

	[Fact]
	public async Task AddAddressAsync_EncryptsBeforeSaving()
	{
		// Arrange
		var options = CreateOptions("AddAddress");

		var aesMock = new Mock<IAesService>();
		aesMock.Setup(a => a.EncryptAsync("plain-email")).ReturnsAsync("enc-email");
		aesMock.Setup(a => a.EncryptAsync("plain-token")).ReturnsAsync("enc-token");

		var accountServiceMock = new Mock<IAccountService>();

		var factory = new TestDbContextFactory(options);
		var repo = new AddressesRepository(factory, aesMock.Object, accountServiceMock.Object);

		var toAdd = new AccountMailBase
		{
			IndexGuidHash = "h",
			Email = "plain-email",
			TokenStorageKey = "plain-token",
			ConnectedAt = DateTime.UtcNow
		};

		// Act
		await repo.AddAddressAsync(toAdd);

		// Assert
		using var verifyCtx = new AppDbContext_Address(options)
		{
			AccountGmail = null!,
			AccountOther = null!,
			AccountMailBase = null!
		};
		verifyCtx.AccountGmail = verifyCtx.Set<AccountGmail>();
		verifyCtx.AccountOther = verifyCtx.Set<AccountOther>();
		verifyCtx.AccountMailBase = verifyCtx.Set<AccountMailBase>();

		var saved = await verifyCtx.AccountMailBase.FirstOrDefaultAsync(a => a.IndexGuidHash == "h",
			cancellationToken: TestContext.Current.CancellationToken);

		Assert.NotNull(saved);
		Assert.Equal("enc-email", saved!.Email);
		Assert.Equal("enc-token", saved.TokenStorageKey);
	}
}
