using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using SmartmailAI.Core.Contracts.Services.Addresses;
using SmartmailAI.Core.Data;
using SmartmailAI.Core.Helpers;
using SmartmailAI.Core.Models;
using RtfPipe;

namespace SmartmailAI.Core.Services.Addresses;

public class GmailApiService : IGmailApiService
{
	public async Task<string> GetEmailAddressAsync(UserCredential credential)
	{
		var service = new GmailService(new BaseClientService.Initializer
		{
			HttpClientInitializer = credential,
			ApplicationName = "SmartmailAI"
		});

		var profile = await service.Users.GetProfile("me").ExecuteAsync();
		return profile.EmailAddress;
	}

	public async Task<List<EmailFromAddress>> GetLastMessagesAsync(UserCredential credential, string MailboxType, bool isAddingNewAddress,
		int? maxResults = 50, DateTime? lastConnection = null)
	{
		var service = new GmailService(new BaseClientService.Initializer
		{
			HttpClientInitializer = credential,
			ApplicationName = "SmartmailAI"
		});

		var request = service.Users.Messages.List("me");
		request.MaxResults = maxResults;
		request.LabelIds = MailboxType.ToUpper();       // TODO: pour récupérer des (vrais) spams/phishings => mettre "SPAM" en valeur
		request.IncludeSpamTrash = false;               // => mettre true en valeur

		if (lastConnection is not null && !isAddingNewAddress)
		{
			var unixSeconds = ToUnixSeconds(lastConnection.Value.AddMinutes(-2));
			request.Q = $"after:{unixSeconds}";
		}

		var response = await request.ExecuteAsync();

		if (response.Messages == null)
			return [];

		var result = new List<EmailFromAddress>();
		string ownerAddress = await GetEmailAddressAsync(credential);

		foreach (var msg in response.Messages)
		{
			var full = await service.Users.Messages.Get("me", msg.Id).ExecuteAsync();

			var (fromName, fromEmail) = ParseEmailAddress(GetHeader(full, "From"));
			var (toNameParsed, toEmailParsed) = ParseListEmailsAddresses(GetHeader(full, "To"));
			var toEmail = MailAddressParserHelper.FormatStringAddresses(toEmailParsed);
			var toName = MailAddressParserHelper.FormatStringAddresses(toNameParsed);
			var date = GetMessageDate(full);

			try
			{
				result.Add(new EmailFromAddress
				{
					Guid = CreateGuid.DeterministicGuid(fromEmail, toEmail, date.ToString()!, ownerAddress).ToString(),
					FromEmail = fromEmail,
					FromName = fromName ?? fromEmail,
					ToEmail = toEmail,
					ToName = toName,
					Cc = GetHeader(full, "Cc"),
					Bcc = GetHeader(full, "Bcc"),
					Subject = GetHeader(full, "Subject"),
					Body = GetMessageBody(full),
					Date = date,
					Owner = ownerAddress,
					MailboxType = MailboxType,
					Attachments = GetAttachments(full)
				});
			}
			catch (DbUpdateException)
			{
				// En cas de doublon (email déjà présent en base), on ignore silencieusement et on continue
				// Cela ne devrait pas arriver car déjà ammorcé par EmailRepository.KeepOnlyNewEmailsAsync()
			}
		}

		return result;
	}

	public async Task SaveAttachmentAsync(UserCredential credential, string messageId, MailAttachment attachment, string destinationFolder)
	{
		var bytes = await DownloadAttachmentAsync(credential, messageId, attachment.AttachmentId);
		var path = Path.Combine(destinationFolder, attachment.FileName);

		await File.WriteAllBytesAsync(path, bytes);
		attachment.FilePath = path; // Met à jour le chemin local
	}

	private static async Task<byte[]> DownloadAttachmentAsync(UserCredential credential, string messageId, string attachmentId)
	{
		var service = new GmailService(new BaseClientService.Initializer
		{
			HttpClientInitializer = credential,
			ApplicationName = "SmartmailAI"
		});

		var attachment = await service.Users.Messages.Attachments
			.Get("me", messageId, attachmentId)
			.ExecuteAsync();

		// Gmail renvoie du Base64 URL-safe — conversion standard
		var base64 = attachment.Data
			.Replace('-', '+')
			.Replace('_', '/');

		return Convert.FromBase64String(base64);
	}

	public async Task SendEmailAsync(UserCredential credential, IEnumerable<string> to, string subject, string body,
		IEnumerable<MailAttachment>? attachments = null, IEnumerable<string>? cc = null, IEnumerable<string>? bcc = null)
	{
		var service = new GmailService(new BaseClientService.Initializer()
		{
			HttpClientInitializer = credential,
			ApplicationName = "SmartmailAI"
		});

		string emailAddressOwner = await GetEmailAddressAsync(credential);

		body = Rtf.ToHtml(body);

		var mimeMessage = MimeHelper.CreateMimeMessage(emailAddressOwner, to, subject, body, attachments ?? [],
			cc ?? [], bcc ?? []);

		var rawMessage = EncodeMessage(mimeMessage);

		var gmailMessage = new Google.Apis.Gmail.v1.Data.Message { Raw = rawMessage };

		await service.Users.Messages.Send(gmailMessage, "me").ExecuteAsync();
	}

	#region Helpers internes au Service (réception des emails)

	private static string GetHeader(Message msg, string name) =>
		msg.Payload.Headers.FirstOrDefault(h => h.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty;

	private static string GetMessageBody(Message message)
	{
		if (!string.IsNullOrEmpty(message.Payload.Body?.Data))
			return DecodeBase64(message.Payload.Body.Data);

		if (message.Payload.Parts != null)
		{
			foreach (var part in message.Payload.Parts)
			{
				if (part.MimeType is "text/plain" or "text/html")
				{
					if (!string.IsNullOrEmpty(part.Body?.Data))
						return DecodeBase64(part.Body.Data);
				}
			}
		}

		return string.Empty;
	}

	private static List<MailAttachment> GetAttachments(Message message)
	{
		var attachments = new List<MailAttachment>();

		if (message.Payload.Parts == null)
			return attachments;

		foreach (var part in message.Payload.Parts)
		{
			// Les pièces joints ont un filename et un attachmendIt
			if (string.IsNullOrEmpty(part.Filename) || string.IsNullOrEmpty(part.Body.AttachmentId))
				continue;

			attachments.Add(new MailAttachment
			{
				FileName = part.Filename,
				AttachmentId = part.Body.AttachmentId,
				MimeType = part.MimeType,
				FileSize = (ulong)(part.Body.Size ?? 0)
			});
		}

		return attachments;
	}

	private static DateTime? GetMessageDate(Message msg)
	{
		return msg.InternalDate is not null ?
			DateTimeOffset.FromUnixTimeMilliseconds(msg.InternalDate!.Value).LocalDateTime : DateTime.UtcNow;
	}

	private static string DecodeBase64(string input)
	{
		input = input.Replace('-', '+').Replace('_', '/');
		var bytes = Convert.FromBase64String(input);
		return Encoding.UTF8.GetString(bytes);
	}

	private static long ToUnixSeconds(DateTime dateTime)
	{
		return new DateTimeOffset(dateTime).ToUnixTimeSeconds();
	}

	private static (string? DisplayName, string Email) ParseEmailAddress(string? rawHeader)
	{
		if (string.IsNullOrWhiteSpace(rawHeader))
			return (null, string.Empty);

		try
		{
			var address = new MailAddress(rawHeader);
			return (string.IsNullOrWhiteSpace(address.DisplayName) ? null : address.DisplayName, address.Address);
		}
		catch
		{
			// Fallback si format non standard
			return (null, rawHeader);
		}
	}

	private static (IEnumerable<string>? DisplayName, IEnumerable<string> Email) ParseListEmailsAddresses(string? rawHeader)
	{
		if (string.IsNullOrWhiteSpace(rawHeader))
			return (null, []);

		try
		{
			var collection = new MailAddressCollection { rawHeader };

			var displayNames = collection
				.Select(x => string.IsNullOrWhiteSpace(x.DisplayName) ? null : x.DisplayName)
				.Where(x => x is not null)!
				.Cast<string>()
				.ToList();

			var emails = collection.Select(x => x.Address).ToList();

			return (displayNames.Count > 0 ? displayNames : null, emails);
		}
		catch
		{
			// Fallback si format non standard
			return (null, [rawHeader]);
		}
	}

	#endregion Helpers internes au Service (réception des emails)

	#region Helpers internes au Service (envoi des emails)

	private static string EncodeMessage(MimeMessage message)
	{
		using var stream = new MemoryStream();
		message.WriteTo(stream);

		var bytes = stream.ToArray();

		return Convert.ToBase64String(bytes)
			.Replace('+', '-')
			.Replace('/', '_')
			.Replace("=", "");
	}

	#endregion Helpers internes au Service (envoi des emails)
}
