using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Graph.Me.SendMail;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client;
using Microsoft.Kiota.Abstractions.Authentication;
using SmartmailAI.Core.Contracts.Services.Addresses;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Services.Addresses;

public class OutlookApiService : IOutlookApiService
{
	private static GraphServiceClient BuildGraphClient(AuthenticationResult authResult)
	{
		// On fournit le token MSAL directement via un provider statique
		var tokenProvider = new StaticTokenProvider(authResult.AccessToken);
		var authProvider = new BaseBearerTokenAuthenticationProvider(tokenProvider);

		return new GraphServiceClient(authProvider);
	}

	public async Task<string> GetEmailAddressAsync(AuthenticationResult authResult)
	{
		var graph = BuildGraphClient(authResult);
		var me = await graph.Me.GetAsync();

		return me?.Mail ?? me?.UserPrincipalName ?? string.Empty;
	}

	public async Task<List<EmailGmail>> GetLastMessagesAsync(AuthenticationResult authResult, string mailboxType, bool isAddingNewAddress,
		int? maxResults = 50, DateTime? lastConnection = null)
	{
		var graph = BuildGraphClient(authResult);
		string owner = await GetEmailAddressAsync(authResult);
		string folderId = ResolveWellKnownFolder(mailboxType);

		string? filter = null;
		if (lastConnection is not null && !isAddingNewAddress)
			filter = $"receivedDateTime ge {lastConnection.Value:yyyy-MM-ddTHH:mm:ssZ}";

		var messages = await graph.Me.MailFolders[folderId].Messages.GetAsync(config =>
		{
			config.QueryParameters.Top = maxResults ?? 50;
			config.QueryParameters.Orderby = ["receivedDateTime desc"];
			config.QueryParameters.Select =
			[
				"id", "subject", "from", "toRecipients", "receivedDateTime", "body", "hasAttachments"
			];

			if (filter is not null)
				config.QueryParameters.Filter = filter;
		});

		if (messages?.Value is null) return [];

		var result = new List<EmailGmail>();

		foreach (var msg in messages.Value)
		{
			var attachments = msg.HasAttachments == true
				? await GetAttachmentsAsync(graph, msg.Id!)
				: [];

			result.Add(new EmailGmail
			{
				Guid = msg.Id!,
				FromEmail = msg.From?.EmailAddress?.Address ?? string.Empty,
				FromName = msg.From?.EmailAddress?.Name,
				ToEmail = msg.ToRecipients?.FirstOrDefault()?.EmailAddress?.Address ?? string.Empty,
				ToName = msg.ToRecipients?.FirstOrDefault()?.EmailAddress?.Name,
				Subject = msg.Subject ?? string.Empty,
				Body = msg.Body?.Content ?? string.Empty,
				Date = msg.ReceivedDateTime?.LocalDateTime,
				Owner = owner,
				MailboxType = mailboxType,
				Attachments = attachments
			});
		}

		return result;
	}

	public async Task SendEmailAsync(AuthenticationResult authResult, string to, string subject, string body,
		IEnumerable<MailAttachment>? attachments = null)
	{
		var graph = BuildGraphClient(authResult);

		var message = new Message
		{
			Subject = subject,
			Body = new ItemBody { ContentType = BodyType.Text, Content = body },
			ToRecipients =
			[
				new Recipient { EmailAddress = new EmailAddress { Address = to } }
			]
		};

		// Pièces jointes
		var attachmentList = attachments?.ToList() ?? [];
		if (attachmentList.Count > 0)
		{
			message.Attachments = [];
			foreach (var att in attachmentList)
			{
				var bytes = await File.ReadAllBytesAsync(att.FilePath);
				message.Attachments.Add(new FileAttachment
				{
					Name = att.FileName,
					ContentType = att.MimeType,
					ContentBytes = bytes
				});
			}
		}

		await graph.Me.SendMail.PostAsync(new SendMailPostRequestBody
		{
			Message = message,
			SaveToSentItems = true
		});
	}

	public async Task SaveAttachmentAsync(AuthenticationResult authResult, string messageId, MailAttachment attachment, string destinationFolder)
	{
		var graph = BuildGraphClient(authResult);

		var att = await graph.Me.Messages[messageId].Attachments[attachment.AttachmentId].GetAsync() as FileAttachment;

		if (att?.ContentBytes is null) return;

		var path = Path.Combine(destinationFolder, attachment.FileName);
		await File.WriteAllBytesAsync(path, att.ContentBytes);
		attachment.FilePath = path;
	}

	#region Helpers internes au Service (réception des emails)

	private static async Task<List<MailAttachment>> GetAttachmentsAsync(GraphServiceClient graph, string messageId)
	{
		var response = await graph.Me.Messages[messageId].Attachments.GetAsync();
		if (response?.Value is null) return [];

		return [.. response.Value
			.OfType<FileAttachment>()
			.Select(a => new MailAttachment
			{
				FileName = a.Name ?? string.Empty,
				AttachmentId = a.Id ?? string.Empty,
				MimeType = a.ContentType ?? string.Empty,
				FileSize = (ulong)(a.Size ?? 0)
			})
		];
	}

	#endregion Helpers internes au Service (réception des emails)

	// Mappe tes labels internes (INBOX, SENT, SPAM…) vers les dossiers Graph bien connus.
	private static string ResolveWellKnownFolder(string mailboxType) =>
		mailboxType.ToUpperInvariant() switch
		{
			"INBOX" => "inbox",
			"SENT" => "sentitems",
			"SPAM" => "junkemail",
			"TRASH" => "deleteditems",
			"DRAFTS" => "drafts",
			_ => "inbox"
		};
}

//  Provider MSAL → Graph : injecte le Bearer token statique
internal sealed class StaticTokenProvider(string accessToken) : IAccessTokenProvider
{
	public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null,
		CancellationToken cancellationToken = default) => Task.FromResult(accessToken);

	public AllowedHostsValidator AllowedHostsValidator { get; } = new(["graph.microsoft.com"]);
}
