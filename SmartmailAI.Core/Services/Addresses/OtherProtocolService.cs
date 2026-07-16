using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using SmartmailAI.Core.Contracts.Services.Addresses;
using SmartmailAI.Core.Data;
using SmartmailAI.Core.Helpers;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Services.Addresses;

public class OtherProtocolService(IOtherTokenStore otherTokenStore) : IOtherProtocolService
{
	private readonly IOtherTokenStore _otherTokenStore = otherTokenStore;

	public async Task<List<EmailFromAddress>?> GetLastMessagesAsync(AccountOther account, string mailboxType, int? maxResults = 300,
		DateTime? lastConnection = null)
	{
		using var client = new ImapClient();
		IMailFolder? folder;
		List<UniqueId>? latestUids = [];

		try // On cherche surtout à tester si il y a une absence/perte de connexion internet au moment de la récupération d'emails
		{
			await client.ConnectAsync(account.ImapHost, account.ImapPort, account.ImapUseSsl);

			string? password = await _otherTokenStore.GetPasswordAsync(account.TokenStorageKey);
			if (password is null) return [];

			await client.AuthenticateAsync(account.Email, password);

			folder = await GetFolderAsync(client, mailboxType);
			await folder.OpenAsync(FolderAccess.ReadOnly);

			var query = SearchQuery.All;

			if (lastConnection is not null)
			{
				query = query.And(SearchQuery.DeliveredAfter(lastConnection.Value));
			}

			var uids = await folder.SearchAsync(query);
			latestUids = [.. uids.TakeLast(maxResults ?? 300).Reverse()];
		}
		catch (Exception) // Si ça plante alors ça vient généralement d'une absence d'internet : System.Net.Http.HttpRequestException
		{
			return null;
		}

		var result = new List<EmailFromAddress>();

		foreach (var uid in latestUids)
		{
			var message = await folder.GetMessageAsync(uid);

			var from = message.From.Mailboxes.FirstOrDefault();
			var to = message.To.Mailboxes;
			var Cc = message.Cc.Mailboxes;
			var Bcc = message.Bcc.Mailboxes;

			try
			{
				var fromEmail = from?.Address ?? string.Empty;
				var toEmail = MailAddressParserHelper.FormatStringAddresses(to?.Select(m => m.Address));
				var toName = MailAddressParserHelper.FormatStringAddresses(to?.Select(m => m.Name!));
				var cc = MailAddressParserHelper.FormatStringAddresses(Cc.Select(m => m.Address));
				var bcc = MailAddressParserHelper.FormatStringAddresses(Bcc.Select(m => m.Address));
				var date = message.Date.LocalDateTime;
				var ownerAddress = account.Email;

				// Pour récupérer les pièces jointes, le messageId doit contenir le UID du message dans la boîte de réception.
				// Pour se conformer à cela, on place le UID du message à la fin de string, juste après la génération du Guid par
				// CreateGuid.DeterministicGuid
				var guid = CreateGuid.DeterministicGuid(fromEmail, toEmail, date.ToString(), ownerAddress).ToString();

				result.Add(new EmailFromAddress
				{
					Guid = String.Concat(guid, "-", uid.Id.ToString()),
					FromEmail = fromEmail,
					FromName = from?.Name ?? fromEmail,
					ToEmail = toEmail,
					ToName = toName,
					Cc = cc,
					Bcc = bcc,
					Subject = message.Subject ?? string.Empty,
					Body = message.HtmlBody ?? message.TextBody ?? string.Empty,
					Date = date,
					Owner = ownerAddress,
					MailboxType = mailboxType,
					Attachments = GetAttachments(message)
				});
			}
			catch (DbUpdateException)
			{
				// En cas de doublon (email déjà présent en base), on ignore silencieusement et on continue
				// Cela ne devrait pas arriver car déjà ammorcé par EmailRepository.KeepOnlyNewEmailsAsync()
			}
		}

		await client.DisconnectAsync(true);

		return result ?? [];
	}

	public async Task SaveAttachmentAsync(AccountOther account, string messageId, MailAttachment attachment, string destinationFolder)
	{
		using var client = new ImapClient();

		await client.ConnectAsync(account.ImapHost, account.ImapPort, account.ImapUseSsl);

		string? password = await _otherTokenStore.GetPasswordAsync(account.TokenStorageKey);
		if (password is null) return;

		await client.AuthenticateAsync(account.Email, password);
		await client.Inbox!.OpenAsync(FolderAccess.ReadOnly);

		// Pour récupérer les pièces jointes, le messageId doit contenir le UID du message dans la boîte de réception.
		// Pour se conformer à cela, on place le UID du message à la fin de string, juste après la génération du Guid par
		// CreateGuid.DeterministicGuid et on le récupère ici en parsant le messageId.
		var trueMessageId = messageId.Split('-').LastOrDefault();

		var uid = new UniqueId(uint.Parse(trueMessageId!));
		var message = await client.Inbox.GetMessageAsync(uid);
		var mimeAttachment = message.Attachments.OfType<MimePart>().FirstOrDefault(x => x.FileName == attachment.FileName);

		if (mimeAttachment is null)
			return;

		var path = Path.Combine(destinationFolder, attachment.FileName);

		await using var stream = File.Create(path);

		await mimeAttachment.Content!.DecodeToAsync(stream);

		attachment.FilePath = path;

		await client.DisconnectAsync(true);
	}

	public async Task SendEmailAsync(AccountOther account, IEnumerable<string> to, string subject, string body,
		IEnumerable<MailAttachment>? attachments = null, IEnumerable<string>? cc = null, IEnumerable<string>? bcc = null)
	{
		var message = MimeHelper.CreateMimeMessage(account.Email, to, subject, body, attachments ?? [],
			cc ?? [], bcc ?? []);

		using var smtp = new SmtpClient();

		await smtp.ConnectAsync(account.SmtpHost, account.SmtpPort, account.SmtpUseSsl
			? SecureSocketOptions.SslOnConnect
			: SecureSocketOptions.StartTls);

		string? password = await _otherTokenStore.GetPasswordAsync(account.TokenStorageKey);
		if (password is null) return;

		await smtp.AuthenticateAsync(account.Email, password);

		await smtp.SendAsync(message);

		await smtp.DisconnectAsync(true);
	}

	#region Helpers internes au Service (envoi des emails)

	private static async Task<IMailFolder> GetFolderAsync(ImapClient client, string mailboxType)
	{
		return mailboxType.ToUpperInvariant() switch
		{
			"INBOX" => await Task.FromResult(client.Inbox!)!,
			"SENT" => await Task.FromResult(client.GetFolder(SpecialFolder.Sent)!),
			"TRASH" => await Task.FromResult(client.GetFolder(SpecialFolder.Trash)!),
			"DRAFTS" => await Task.FromResult(client.GetFolder(SpecialFolder.Drafts)!),
			_ => await Task.FromResult(client.Inbox!)!
		};
	}

	private static List<MailAttachment> GetAttachments(MimeMessage message)
	{
		var result = new List<MailAttachment>();

		foreach (var attachment in message.Attachments)
		{
			if (attachment is not MimePart part)
				continue;

			result.Add(new MailAttachment
			{
				FileName = part.FileName!,
				MimeType = part.ContentType.MimeType,
				FileSize = (ulong)(part.Content?.Stream?.Length ?? 0)
			});
		}

		return result;
	}

	#endregion Helpers internes au Service (envoi des emails)
}
