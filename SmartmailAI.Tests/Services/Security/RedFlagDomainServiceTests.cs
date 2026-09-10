using System.Net;
using System.Reflection;
using SmartmailAI.Core.Services.Security;

namespace SmartmailAI.Tests.Services.Security;

public partial class RedFlagDomainServiceTests
{
	private partial class FakeHandler(Func<int, HttpResponseMessage> responder) : HttpMessageHandler
	{
		private readonly Func<int, HttpResponseMessage> _responder = responder ?? throw new ArgumentNullException(nameof(responder));
		public int CallCount { get; private set; }

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
		{
			CallCount++;
			return Task.FromResult(_responder(CallCount));
		}
	}

	[Fact]
	public async Task IsFlaggedDomainAsync_LoadsFromRemoteAndSavesCache()
	{
		// Arrange
		var content = "# comment\nexample.com\nbad.com\n";
		var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(content)
		});

		var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
		http.DefaultRequestHeaders.Add("Accept", "text/plain");

		var svc = new RedFlagDomainService();

		// override private http client and cache path to a temp file
		var httpField = typeof(RedFlagDomainService).GetField("_http", BindingFlags.NonPublic | BindingFlags.Instance)!;
		httpField.SetValue(svc, http);

		var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".txt");
		var cacheField = typeof(RedFlagDomainService).GetField("_cacheFilePath", BindingFlags.NonPublic | BindingFlags.Instance)!;
		cacheField.SetValue(svc, tempPath);

		// Act
		var isFlagged = await svc.IsFlaggedDomainAsync("example.com");

		// Assert
		Assert.True(isFlagged);
		Assert.True(File.Exists(tempPath));
		var saved = await File.ReadAllTextAsync(tempPath, TestContext.Current.CancellationToken);
		Assert.Contains("example.com", saved);
	}

	[Fact]
	public async Task IsFlaggedDomainAsync_FallsBackToCacheWhenRemoteFails()
	{
		// Arrange
		var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".txt");
		await File.WriteAllTextAsync(tempPath, "offline.com\n", TestContext.Current.CancellationToken);

		var handler = new FakeHandler(_ => throw new HttpRequestException("network"));
		var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };

		var svc = new RedFlagDomainService();
		var httpField = typeof(RedFlagDomainService).GetField("_http", BindingFlags.NonPublic | BindingFlags.Instance)!;
		httpField.SetValue(svc, http);
		var cacheField = typeof(RedFlagDomainService).GetField("_cacheFilePath", BindingFlags.NonPublic | BindingFlags.Instance)!;
		cacheField.SetValue(svc, tempPath);

		// Act
		var isFlagged = await svc.IsFlaggedDomainAsync("offline.com");

		// Assert
		Assert.True(isFlagged);
	}

	[Fact]
	public async Task RefreshAsync_ForceReloadsFromRemote()
	{
		// Arrange
		var first = "old.com\n";
		var second = "new.com\n";
		var handler = new FakeHandler(call =>
		{
			var content = call == 1 ? first : second;
			return new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(content)
			};
		});

		var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
		var svc = new RedFlagDomainService();
		var httpField = typeof(RedFlagDomainService).GetField("_http", BindingFlags.NonPublic | BindingFlags.Instance)!;
		httpField.SetValue(svc, http);

		var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".txt");
		var cacheField = typeof(RedFlagDomainService).GetField("_cacheFilePath", BindingFlags.NonPublic | BindingFlags.Instance)!;
		cacheField.SetValue(svc, tempPath);

		// Act - initial load
		var wasOld = await svc.IsFlaggedDomainAsync("old.com");
		// Act - force refresh
		await svc.RefreshAsync();
		var isNew = await svc.IsFlaggedDomainAsync("new.com");

		// Assert
		Assert.True(wasOld);
		Assert.True(isNew);
	}
}
