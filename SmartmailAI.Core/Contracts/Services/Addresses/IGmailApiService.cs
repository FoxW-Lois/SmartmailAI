using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Contracts.Services.Addresses;

public interface IGmailApiService
{
	Task<string> GetEmailAddressAsync(UserCredential credential);

	Task<List<EmailFromAddress>?> GetLastMessagesAsync(UserCredential credential, string MailboxType, int? maxResults = 300,
		DateTime? lastConnection = null);

	Task SaveAttachmentAsync(UserCredential credential, string messageId, MailAttachment attachment, string destinationFolder);

	Task SendEmailAsync(UserCredential credential, IEnumerable<string> to, string subject, string body, IEnumerable<MailAttachment>? attachments = null,
		IEnumerable<string>? cc = null, IEnumerable<string>? bcc = null);
}
