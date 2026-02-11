using System.Collections.Generic;
using System.Threading.Tasks;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Contracts.Services;

public interface IEmailsService
{
	Task<IEnumerable<MailboxCategory>> GetAllCategoriesAsync();

	Task<IEnumerable<Email>> GetAllEmailsAsync();

	Task<IEnumerable<Email>> GetEmailsByMailboxTypeAsync(MailboxType mailboxType);

	Task MarkEmailAsStarredAsync(Email email);

	Task MarkEmailAsReadAsync(Email email);

	Task MarkEmailAsUnreadAsync(Email email);

	Task MarkEmailAsArchivedAsync(Email email);

	Task RestoreEmailAsync(Email email);

	Task DeleteEmailAsync(Email email);

	Task MarkEmailAsTrashedAsync(Email email);
}
