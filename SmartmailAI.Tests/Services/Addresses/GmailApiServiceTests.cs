using System.Reflection;
using System.Text;
using Google.Apis.Gmail.v1.Data;
using MimeKit;
using SmartmailAI.Core.Models;
using SmartmailAI.Core.Services.Addresses;

namespace SmartmailAI.Tests.Services.Addresses;

public class GmailApiServiceTests
{
	[Fact]
	public void ParseEmailAddress_ReturnsDisplayNameAndEmail_WhenValid()
	{
		// Arrange
		var raw = "John Doe <john@example.com>";

		// Act
		var method = typeof(GmailApiService).GetMethod("ParseEmailAddress", BindingFlags.NonPublic | BindingFlags.Static)!;
		var result = method.Invoke(null, [raw]);

		// Assert
		var type = result!.GetType();
		var display = (string?)type.GetField("Item1")?.GetValue(result);
		var email = (string)type.GetField("Item2")!.GetValue(result)!;

		Assert.Equal("John Doe", display);
		Assert.Equal("john@example.com", email);
	}

	[Fact]
	public void ParseEmailAddress_FallsBack_WhenInvalid()
	{
		// Arrange
		var raw = "not-an-email";

		// Act
		var method = typeof(GmailApiService).GetMethod("ParseEmailAddress", BindingFlags.NonPublic | BindingFlags.Static)!;
		var result = method.Invoke(null, [raw]);

		// Assert
		var type = result!.GetType();
		var display = (string?)type.GetField("Item1")?.GetValue(result);
		var email = (string)type.GetField("Item2")!.GetValue(result)!;

		Assert.Null(display);
		Assert.Equal(raw, email);
	}

	[Fact]
	public void ParseListEmailsAddresses_ParsesMultipleAddresses()
	{
		// Arrange
		var raw = "Alice <alice@example.com>, bob@example.com";

		// Act
		var method = typeof(GmailApiService).GetMethod("ParseListEmailsAddresses", BindingFlags.NonPublic | BindingFlags.Static)!;
		var result = method.Invoke(null, new object?[] { raw });

		// Assert
		var type = result!.GetType();
		var item1 = (IEnumerable<string>?)type.GetField("Item1")?.GetValue(result);
		var item2 = (IEnumerable<string>)type.GetField("Item2")!.GetValue(result)!;

		Assert.NotNull(item1);
		Assert.Contains("Alice", item1!);
		Assert.Equal(2, item2.Count());
		Assert.Contains("alice@example.com", item2);
		Assert.Contains("bob@example.com", item2);
	}

	[Fact]
	public void DecodeBase64_DecodesStandardBase64()
	{
		// Arrange
		var text = "hello world";
		var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(text));

		// Act
		var method = typeof(GmailApiService).GetMethod("DecodeBase64", BindingFlags.NonPublic | BindingFlags.Static)!;
		var result = (string)method.Invoke(null, [base64])!;

		// Assert
		Assert.Equal(text, result);
	}

	[Fact]
	public void FindPart_FindsAndDecodesPart()
	{
		// Arrange
		var payload = new Google.Apis.Gmail.v1.Data.MessagePart
		{
			MimeType = "text/plain",
			Body = new() { Data = Convert.ToBase64String(Encoding.UTF8.GetBytes("plain body")) }
		};

		// Act
		var method = typeof(GmailApiService).GetMethod("FindPart", BindingFlags.NonPublic | BindingFlags.Static)!;
		var result = (string?)method.Invoke(null, [payload, "text/plain"]);

		// Assert
		Assert.Equal("plain body", result);
	}

	[Fact]
	public void GetMessageBody_ReturnsPlainWhenHtmlMissing()
	{
		// Arrange
		var msg = new Google.Apis.Gmail.v1.Data.Message
		{
			Payload = new()
			{
				Parts =
				[
					new() {
						MimeType = "text/plain",
						Body = new () {
							Data = Convert.ToBase64String(Encoding.UTF8.GetBytes("only text"))
						}
					}
				]
			}
		};

		// Act
		var method = typeof(GmailApiService).GetMethod("GetMessageBody", BindingFlags.NonPublic | BindingFlags.Static)!;
		var result = (string)method.Invoke(null, [msg])!;

		// Assert
		Assert.Equal("only text", result);
	}

	[Fact]
	public void GetAttachments_ReturnsAttachmentsFromParts()
	{
		// Arrange
		var msg = new Google.Apis.Gmail.v1.Data.Message
		{
			Payload = new()
			{
				Parts =
				[
					new()
					{
						Filename = "file.txt",
						MimeType = "text/plain",
						Body = new() { AttachmentId = "att-1", Size = 123 }
					},
					new() // should be ignored
					{
						Filename = string.Empty,
						MimeType = "text/plain",
						Body = new() { AttachmentId = string.Empty }
					}
				]
			}
		};

		// Act
		var method = typeof(GmailApiService).GetMethod("GetAttachments", BindingFlags.NonPublic | BindingFlags.Static)!;
		var result = (List<MailAttachment>)method.Invoke(null, new object?[] { msg })!;

		// Assert
		Assert.Single(result);
		var att = result[0];
		Assert.Equal("file.txt", att.FileName);
		Assert.Equal("att-1", att.AttachmentId);
		Assert.Equal((ulong)123, att.FileSize);
	}

	[Fact]
	public void GetMessageDate_ConvertsInternalDateMilliseconds()
	{
		// Arrange
		var ms = 1000L; // 1 second after epoch
		var msg = new Message { InternalDate = ms };

		// Act
		var method = typeof(GmailApiService).GetMethod("GetMessageDate", BindingFlags.NonPublic | BindingFlags.Static)!;
		var result = (DateTime?)method.Invoke(null, [msg]);

		// Assert
		var expected = DateTimeOffset.FromUnixTimeMilliseconds(ms).LocalDateTime;
		Assert.Equal(expected, result);
	}

	[Fact]
	public void ToUnixSeconds_ReturnsExpected()
	{
		// Arrange
		var dt = DateTimeOffset.FromUnixTimeSeconds(12345).UtcDateTime;

		// Act
		var method = typeof(GmailApiService).GetMethod("ToUnixSeconds", BindingFlags.NonPublic | BindingFlags.Static)!;
		var result = (long)method.Invoke(null, [dt])!;

		// Assert
		Assert.Equal(12345L, result);
	}

	[Fact]
	public void EncodeMessage_ProducesExpectedBase64UrlSafeString()
	{
		// Arrange
		var msg = new MimeMessage();
		msg.From.Add(new MailboxAddress("Me", "me@example.com"));
		msg.To.Add(new MailboxAddress("You", "you@example.com"));
		msg.Subject = "sub";
		msg.Body = new TextPart("plain") { Text = "body" };

		using var ms = new MemoryStream();
		msg.WriteTo(ms, TestContext.Current.CancellationToken);
		var bytes = ms.ToArray();
		var expected = Convert.ToBase64String(bytes).Replace('+', '-')
			.Replace('/', '_').Replace("=", string.Empty);

		// Act
		var method = typeof(GmailApiService).GetMethod("EncodeMessage", BindingFlags.NonPublic | BindingFlags.Static)!;
		var result = (string)method.Invoke(null, [msg])!;

		// Assert
		Assert.Equal(expected, result);
	}
}
