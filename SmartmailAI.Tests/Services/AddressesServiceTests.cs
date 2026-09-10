using Moq;
using SmartmailAI.Core.Contracts.Repository;
using SmartmailAI.Core.Contracts.Services.Addresses;
using SmartmailAI.Core.Models;
using SmartmailAI.Core.Services;

namespace SmartmailAI.Tests.Services;

public class AddressesServiceTests
{
	[Fact]
	public async Task RefreshAddressesListAsync_SetsHasAnyAndRaisesEvent()
	{
		// Arrange
		var repoMock = new Mock<IAddressesRepository>();
		repoMock.Setup(r => r.GetAllAddressesByAccountIndexGuidAsync()).ReturnsAsync([]);

		var svc = new AddressesService(repoMock.Object, Mock.Of<IEmailRepository>(), Mock.Of<IGmailCredentialService>(),
			Mock.Of<IGmailApiService>(), Mock.Of<IGmailLogoutService>(), Mock.Of<IOtherCredentialService>(), Mock.Of<IOtherLogoutService>(),
			Mock.Of<IOtherTokenStore>());

		bool? eventValue = null;
		svc.AddressesListChanged += (_, val) => eventValue = val;

		// Act
		await svc.RefreshAddressesListAsync();

		// Assert
		Assert.False(svc.HasAny);
		Assert.NotNull(eventValue);

		// Arrange - now with one item
		repoMock.Setup(r => r.GetAllAddressesByAccountIndexGuidAsync()).ReturnsAsync([new() {
			IndexGuidHash = "h", Email = "e", TokenStorageKey = "t", ConnectedAt = DateTime.UtcNow
		}]);

		// Act
		await svc.RefreshAddressesListAsync();

		// Assert
		Assert.True(svc.HasAny);
	}

	[Fact]
	public async Task RemoveAddressAsync_CallsLogoutAndDeletes()
	{
		// Arrange
		var addressesRepo = new Mock<IAddressesRepository>();
		addressesRepo.Setup(r => r.DeleteAddressAsync(It.IsAny<AccountMailBase>())).Returns(Task.CompletedTask);

		var emailRepo = new Mock<IEmailRepository>();
		emailRepo.Setup(e => e.DeleteAllEmailsAsync(It.IsAny<AccountMailBase>())).Returns(Task.CompletedTask);

		var gmailLogout = new Mock<IGmailLogoutService>();
		var otherLogout = new Mock<IOtherLogoutService>();

		var svc = new AddressesService(addressesRepo.Object, emailRepo.Object, Mock.Of<IGmailCredentialService>(),
			Mock.Of<IGmailApiService>(), gmailLogout.Object, Mock.Of<IOtherCredentialService>(), otherLogout.Object,
			Mock.Of<IOtherTokenStore>());

		var gmailAccount = new AccountGmail
		{
			Email = "g@mail",
			IndexGuidHash = "h",
			TokenStorageKey = "t",
			ConnectedAt = DateTime.UtcNow,
			GoogleUserId = "u"
		};

		// Act
		var res = await svc.RemoveAddressAsync(gmailAccount);

		// Assert
		Assert.True(res);
		gmailLogout.Verify(g => g.LogoutAsync(It.IsAny<AccountGmail>()), Times.Once);
		emailRepo.Verify(e => e.DeleteAllEmailsAsync(It.IsAny<AccountMailBase>()), Times.Once);
		addressesRepo.Verify(a => a.DeleteAddressAsync(It.IsAny<AccountMailBase>()), Times.Once);

		// Other account
		var otherAccount = new AccountOther
		{
			Email = "o@mail",
			IndexGuidHash = "h2",
			TokenStorageKey = "t2",
			ConnectedAt = DateTime.UtcNow,
			Password = string.Empty,
			UserName = "u",
			ImapHost = "i",
			ImapPort = 1,
			SmtpHost = "s",
			SmtpPort = 2
		};

		// Act
		var res2 = await svc.RemoveAddressAsync(otherAccount);

		// Assert
		Assert.True(res2);
		otherLogout.Verify(o => o.LogoutAsync(It.IsAny<AccountOther>()), Times.Once);
		emailRepo.Verify(e => e.DeleteAllEmailsAsync(It.IsAny<AccountMailBase>()), Times.Exactly(2));
		addressesRepo.Verify(a => a.DeleteAddressAsync(It.IsAny<AccountMailBase>()), Times.Exactly(2));
	}

	[Fact]
	public async Task GetAccountByEmailAsync_ReturnsAccount()
	{
		// Arrange
		var addressesRepo = new Mock<IAddressesRepository>();
		addressesRepo.Setup(r => r.GetAddressByEmailAsync("a@b.com")).ReturnsAsync(new AccountMailBase
		{
			Email = "a@b.com",
			IndexGuidHash = "h",
			TokenStorageKey = "t",
			ConnectedAt = DateTime.UtcNow
		});

		var svc = new AddressesService(addressesRepo.Object, Mock.Of<IEmailRepository>(), Mock.Of<IGmailCredentialService>(),
			Mock.Of<IGmailApiService>(), Mock.Of<IGmailLogoutService>(), Mock.Of<IOtherCredentialService>(), Mock.Of<IOtherLogoutService>(),
			Mock.Of<IOtherTokenStore>());

		// Act
		var acc = await svc.GetAccountByEmailAsync("a@b.com");

		// Assert
		Assert.NotNull(acc);
		Assert.Equal("a@b.com", acc!.Email);
	}

	[Fact]
	public async Task GetListAccountsLinkedAsync_ReturnsList()
	{
		// Arrange
		var list = new List<AccountMailBase> { new()
		{
			Email = "x@x",
			IndexGuidHash = "h",
			TokenStorageKey = "t",
			ConnectedAt = DateTime.UtcNow
		}};
		var addressesRepo = new Mock<IAddressesRepository>();
		addressesRepo.Setup(r => r.GetAllAddressesByAccountIndexGuidAsync()).ReturnsAsync(list);

		var svc = new AddressesService(addressesRepo.Object, Mock.Of<IEmailRepository>(), Mock.Of<IGmailCredentialService>(),
			Mock.Of<IGmailApiService>(), Mock.Of<IGmailLogoutService>(), Mock.Of<IOtherCredentialService>(), Mock.Of<IOtherLogoutService>(),
			Mock.Of<IOtherTokenStore>());

		// Act
		var res = await svc.GetListAccountsLinkedAsync();

		// Assert
		Assert.Single(res);
	}
}
