using System.Collections.Generic;
using System.IO;
using System.Linq;
using MimeKit;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Helpers;

public sealed class MimeHelper
{
	public static MimeMessage CreateMimeMessage(string from, IEnumerable<string> to, string subject, string body, IEnumerable<MailAttachment> attachments,
		IEnumerable<string> cc, IEnumerable<string> bcc)
	{
		var message = new MimeMessage();

		message.From.Add(MailboxAddress.Parse(from));
		message.To.AddRange(to.SelectMany(MailAddressParserHelper.ParseAddresses));
		message.Cc.AddRange(cc.SelectMany(MailAddressParserHelper.ParseAddresses));
		message.Bcc.AddRange(bcc.SelectMany(MailAddressParserHelper.ParseAddresses));
		message.Subject = subject;

		var builder = new BodyBuilder { TextBody = body };

		foreach (var attachment in attachments)
		{
			if (string.IsNullOrWhiteSpace(attachment.FilePath))
				continue;

			if (!File.Exists(attachment.FilePath))
				continue;

			builder.Attachments.Add(attachment.FilePath);
		}

		message.Body = builder.ToMessageBody();

		return message;
	}
}
