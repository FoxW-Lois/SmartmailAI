using System.Reflection;
using MailKit;
using MailKit.Net.Imap;
using MimeKit;
using Moq;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Tests.Services.Addresses;

public partial class OtherProtocolServiceTests
{
	private partial class TestImapClient(IMailFolder inbox, IMailFolder sent) : ImapClient
	{
		private readonly IMailFolder _inbox = inbox;
		private readonly IMailFolder _sent = sent;

		public override IMailFolder Inbox => _inbox;

		public override IMailFolder GetFolder(SpecialFolder folder)
		{
			return folder == SpecialFolder.Sent ? _sent : _inbox;
		}
	}

	[Fact]
	public async Task GetFolderAsync_Returns_Inbox_For_INBOX()
	{
		// Arrange
		var inboxMock = new Mock<IMailFolder>();
		var sentMock = new Mock<IMailFolder>();
		var client = new TestImapClient(inboxMock.Object, sentMock.Object);

		// Act
		var method = typeof(SmartmailAI.Core.Services.Addresses.OtherProtocolService)
			.GetMethod("GetFolderAsync", BindingFlags.NonPublic | BindingFlags.Static)!;

		var task = (Task<IMailFolder>)method.Invoke(null, [client, "INBOX"])!;
		var result = await task;

		// Assert
		Assert.Same(inboxMock.Object, result);
	}

	[Fact]
	public async Task GetFolderAsync_Returns_Sent_For_SENT()
	{
		// Arrange
		var inboxMock = new Mock<IMailFolder>();
		var sentMock = new Mock<IMailFolder>();
		var client = new TestImapClient(inboxMock.Object, sentMock.Object);

		// Act
		var method = typeof(SmartmailAI.Core.Services.Addresses.OtherProtocolService)
			.GetMethod("GetFolderAsync", BindingFlags.NonPublic | BindingFlags.Static)!;

		var task = (Task<IMailFolder>)method.Invoke(null, [client, "SeNt"])!; // case-insensitive
		var result = await task;

		// Assert
		Assert.Same(sentMock.Object, result);
	}

	[Fact]
	public void GetAttachments_Returns_Only_MimePart_Attachments()
	{
		// Arrange
		var message = new MimeMessage();
		var multipart = new Multipart("mixed");

		// Create a MimePart attachment
		var contentBytes = new byte[] { 1, 2, 3 };
		var part = new MimePart("application", "octet-stream")
		{
			Content = new MimeContent(new MemoryStream(contentBytes)),
			ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
			FileName = "file.bin"
		};

		// Create a non-MimePart attachment (MessagePart)
		var messagePart = new MessagePart { Message = new MimeMessage() };

		multipart.Add(part);
		multipart.Add(messagePart);
		message.Body = multipart;

		// Act
		var method = typeof(SmartmailAI.Core.Services.Addresses.OtherProtocolService)
			.GetMethod("GetAttachments", BindingFlags.NonPublic | BindingFlags.Static)!;

		var attachments = (System.Collections.Generic.List<MailAttachment>)method.Invoke(null, [message])!;

		// Assert
		var single = Assert.Single(attachments);
		Assert.Equal("file.bin", single.FileName);
		Assert.Equal("application/octet-stream", single.MimeType);
		Assert.Equal((ulong)contentBytes.Length, single.FileSize);
	}
}
