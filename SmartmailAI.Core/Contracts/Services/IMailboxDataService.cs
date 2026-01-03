using System.Collections.Generic;
using System.Threading.Tasks;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Contracts.Services;

public interface IMailboxDataService
{
	Task<IEnumerable<MailboxCategory>> GetAllCategoriesAsync();

	Task<IEnumerable<Email>> GetListDetails_AllEmailsAsync();

	Task<IEnumerable<Email>> GetEmailsByMailboxTypeAsync(MailboxType mailboxType);

	Task MarkEmailAsReadAsync(Email email);

	Task MarkEmailAsUnreadAsync(Email email);

	Task DeleteEmailAsync(Email email);

	Task MarkEmailAsTrashedAsync(Email email);

	Task MarkEmailAsArchivedAsync(Email email);

	Task MarkEmailAsStarredAsync(Email email);
}
