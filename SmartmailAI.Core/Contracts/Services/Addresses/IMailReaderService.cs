using System.Collections.Generic;
using System.Threading.Tasks;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Contracts.Services.Addresses;

public interface IMailReaderService
{
	Task<IReadOnlyList<Email>> GetLastMessagesFromAccountAsync(AccountGmail accountGmail, bool isAddingNewAddress);

	Task SaveAttachmentFromEmailAsync(AccountGmail accountGmail, string messageId, MailAttachment attachment, string destinationFolder);
}
