using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Contracts.Services.Addresses;

public interface IGmailApiService
{
	Task<string> GetEmailAddressAsync(UserCredential credential);

	Task<List<EmailGmail>> GetLastMessagesAsync(UserCredential credential, string MailboxType, bool isAddingNewAddress, int? maxResults = 50,
		DateTime? lastConnection = null);

	Task SaveAttachmentAsync(UserCredential credential, string messageId, MailAttachment attachment, string destinationFolder);

	Task SendEmailAsync(UserCredential credential, string to, string subject, string body, IEnumerable<MailAttachment>? attachments = null);
}
