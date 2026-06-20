using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Contracts.Services.Addresses;

public interface IOtherProtocolService
{
	Task<List<EmailFromAddress>?> GetLastMessagesAsync(AccountOther account, string mailboxType, bool isAddingNewAddress, int? maxResults = 50,
		DateTime? lastConnection = null);

	Task SaveAttachmentAsync(AccountOther account, string messageId, MailAttachment attachment, string destinationFolder);

	Task SendEmailAsync(AccountOther account, IEnumerable<string> to, string subject, string body, IEnumerable<MailAttachment>? attachments = null,
		IEnumerable<string>? cc = null, IEnumerable<string>? bcc = null);
}
