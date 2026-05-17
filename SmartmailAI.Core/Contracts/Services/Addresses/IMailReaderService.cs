using System.Collections.Generic;
using System.Threading.Tasks;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Contracts.Services.Addresses;

public interface IMailReaderService
{
	Task<IReadOnlyList<Email>> GetLastMessagesFromAccountAsync(bool isAddingNewAddress, AccountGmail? accountGmail = null,
		AccountOther? accountOther = null);

	Task SaveAttachmentFromEmailAsync(string messageId, MailAttachment attachment, string destinationFolder, AccountGmail? accountGmail = null,
		AccountOther? accountOther = null);
}
