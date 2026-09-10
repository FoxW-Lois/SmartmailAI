using System.Reflection;
using SmartmailAI.Core.Models;
using SmartmailAI.Core.Models.Security;
using SmartmailAI.Core.Services.Security;

namespace SmartmailAI.Tests.Services.Security;

public partial class VirusTotalServiceTests
{
	private partial class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
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
	public async Task AnalyzeAttachmentsAsync_NonSuspiciousExtension_ReturnsEmpty()
	{
		// Arrange
		var svc = new VirusTotalService();
		var attachments = new List<MailAttachment>
		{
			new() { FileName = "readme.txt" }
		};

		// Act
		var results = await svc.AnalyzeAttachmentsAsync(attachments);

		// Assert
		Assert.NotNull(results);
		Assert.Empty(results);
	}

	[Fact]
	public async Task AnalyzeAttachmentsAsync_NoApiKey_ReturnsLocalResult_ForDangerousExtension()
	{
		// Arrange
		var svc = new VirusTotalService(apiKey: null);
		var attachments = new List<MailAttachment>
		{
			new() { FileName = "dangerous.EXE" }
		};

		// Act
		var results = await svc.AnalyzeAttachmentsAsync(attachments);

		// Assert
		Assert.Single(results);
		var r = results[0];
		Assert.Equal("dangerous.EXE", r.FileName);
		Assert.True(r.IsMalicious);
		Assert.Equal(-1, r.MaliciousCount);
	}

	[Fact]
	public async Task ParseSearchResponse_ValidJson_ReturnsCorrectResult()
	{
		// Arrange
		var ct = TestContext.Current.CancellationToken;
		await Task.Delay(0, ct); // ensure cancellation token is referenced to avoid warnings
		string fileName = "test.exe";
		string json = @"{ ""data"": [ { ""id"": ""abcdef123456"", ""attributes"": { ""last_analysis_stats"": { ""harmless"": 10, ""malicious"": 2, ""suspicious"": 1, ""undetected"": 3 } } } ] }";

		// Act
		var method = typeof(VirusTotalService).GetMethod("ParseSearchResponse", BindingFlags.NonPublic | BindingFlags.Static);
		Assert.NotNull(method);
		var result = method!.Invoke(null, [fileName, json]) as VirusTotalResult;

		// Assert
		Assert.NotNull(result);
		Assert.Equal(fileName, result!.FileName);
		Assert.True(result.IsMalicious);
		Assert.Equal(3, result.MaliciousCount); // malicious + suspicious
		Assert.Equal(16, result.TotalEngines); // harmless + malicious + suspicious + undetected = 10+2+1+3
		Assert.Equal("https://www.virustotal.com/gui/file/abcdef123456", result.Permalink);
	}

	[Fact]
	public async Task ParseSearchResponse_EmptyData_ReturnsNull()
	{
		// Arrange
		var ct = TestContext.Current.CancellationToken;
		await Task.Delay(0, ct); // ensure cancellation token is referenced to avoid warnings
		string fileName = "nothing.bin";
		string json = "{ \"data\": [] }";

		// Act
		var method = typeof(VirusTotalService).GetMethod("ParseSearchResponse", BindingFlags.NonPublic | BindingFlags.Static);
		Assert.NotNull(method);
		var result = method!.Invoke(null, [fileName, json]) as VirusTotalResult;

		// Assert
		Assert.Null(result);
	}
}
