using System.Collections.Generic;
using System.Threading.Tasks;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Contracts.Services.Addresses;

public interface IMailReaderService
{
	// Obsolète
	Task<IReadOnlyList<EmailGmail>> GetLastMessagesFromFirstAccountAsync();

	Task<IReadOnlyList<EmailGmail>> GetLastMessagesFromAccountAsync(AccountGmail accountGmail);
}
