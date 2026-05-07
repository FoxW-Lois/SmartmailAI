using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Contracts.Services.Addresses;

public interface IOutlookApiService
{
	Task<string> GetEmailAddressAsync(AuthenticationResult authResult);

	Task<List<EmailGmail>> GetLastMessagesAsync(AuthenticationResult authResult, string MailboxType, bool isAddingNewAddress, int? maxResults = 50,
		DateTime? lastConnection = null);

	Task SaveAttachmentAsync(AuthenticationResult authResult, string messageId, MailAttachment attachment, string destinationFolder);

	Task SendEmailAsync(AuthenticationResult authResult, string to, string subject, string body, IEnumerable<MailAttachment>? attachments = null);
}
