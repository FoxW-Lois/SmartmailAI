using System.Net;
using System.Reflection;
using SmartmailAI.Core.Models.Security;
using SmartmailAI.Core.Services;

namespace SmartmailAI.Tests.Services.Security;

public partial class DnsSecurityServiceTests
{
	// A fake HttpMessageHandler to simule DoH responses of Cloudflare
	private partial class FakeDohHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
	{
		private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder = responder
			?? throw new ArgumentNullException(nameof(responder));
		public int CallCount { get; private set; }

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
		{
			CallCount++;
			return Task.FromResult(_responder(request));
		}
	}

	[Fact]
	public async Task CheckDomainAsync_InvalidEmail_ReturnsUnknown()
	{
		// Arrange
		var svc = new DnsSecurityService();

		// Act
		var res = await svc.CheckDomainAsync("not-an-email");

		// Assert
		Assert.NotNull(res);
		Assert.Equal(SpfStatus.Unknown, res.SpfStatus);
		Assert.Equal(DmarcStatus.Unknown, res.DmarcStatus);
	}

	[Fact]
	public async Task CheckDomainAsync_WithSpfAndDmarc_ReturnsNotSuspicious()
	{
		// Arrange
		var domain = "example.com";

		string spfJson = $"{{\"Status\":0,\"Answer\":[{{\"name\":\"{domain}.\",\"type\":16,\"TTL\":3600,\"data\":\"\\\"v=spf1 include:_spf.example.com -all\\\"\"}}]}}";
		string dmarcJson = $"{{\"Status\":0,\"Answer\":[{{\"name\":\"_dmarc.{domain}.\",\"type\":16,\"TTL\":3600,\"data\":\"\\\"v=DMARC1; p=reject\\\"\"}}]}}";

		var handler = new FakeDohHandler(req =>
		{
			var q = req.RequestUri?.Query ?? string.Empty;
			var namePair = System.Uri.UnescapeDataString(q).Split('&', StringSplitOptions.RemoveEmptyEntries);
			string? name = null;
			foreach (var p in namePair)
			{
				var part = p.TrimStart('?');
				if (part.StartsWith("name=", StringComparison.OrdinalIgnoreCase))
				{
					name = part.Substring(5);
					break;
				}
			}

			var json = name == $"_dmarc.{domain}" ? dmarcJson : spfJson;
			return new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(json)
			};
		});

		var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
		http.DefaultRequestHeaders.Add("Accept", "application/dns-json");

		var svc = new DnsSecurityService();
		// Remplacer le HttpClient interne via reflection
		var field = typeof(DnsSecurityService).GetField("_http", BindingFlags.NonPublic | BindingFlags.Instance)!;
		field.SetValue(svc, http);

		// Act
		var res = await svc.CheckDomainAsync($"user@{domain}");

		// Assert
		Assert.NotNull(res);
		Assert.Equal(domain, res.Domain);
		Assert.Equal(SpfStatus.Pass, res.SpfStatus);
		Assert.Equal(DmarcStatus.Present, res.DmarcStatus);
		Assert.False(res.IsSuspicious);
	}

	[Fact]
	public async Task CheckDomainAsync_MissingSpfOrDmarc_ReturnsSuspicious_And_UsesCache()
	{
		// Arrange
		var domain = "nodns.example";
		// SPF missing -> return Status 0 but Answer empty
		string emptyJson = "{\"Status\":0}";

		var handler = new FakeDohHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(emptyJson)
		});

		var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
		http.DefaultRequestHeaders.Add("Accept", "application/dns-json");

		var svc = new DnsSecurityService();
		var field = typeof(DnsSecurityService).GetField("_http", BindingFlags.NonPublic | BindingFlags.Instance)!;
		field.SetValue(svc, http);

		// Act - First request
		var res1 = await svc.CheckDomainAsync($"a@{domain}");
		// Act - Second request (must be served from the cache)
		var res2 = await svc.CheckDomainAsync($"b@{domain}");

		// Assert
		Assert.NotNull(res1);
		Assert.True(res1.IsSuspicious);
		Assert.Equal(SpfStatus.None, res1.SpfStatus);

		Assert.Same(res1, res2); // Instance cached ; Two request DoH are doing on the first invoke : SPF + DMARC
		Assert.Equal(2, handler.CallCount);
	}
}
