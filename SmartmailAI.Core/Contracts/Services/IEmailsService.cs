using System.Collections.Generic;
using System.Threading.Tasks;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Contracts.Services;

public interface IEmailsService
{
	Task<IEnumerable<MailboxCategory>> GetAllCategoriesAsync();

	Task<(IEnumerable<Email>, int totalCount)> GetMailboxEmailsAsync(MailboxType mailboxType, string? addressAccount, int page, int pageSize);

	Task ScribbleEmailAsync(string? guid, string from, string? to, string? subject, string? body, string? cc, string? bcc);

	Task MarkEmailAsStarredAsync(Email email);

	Task MarkEmailAsReadAsync(Email email);

	Task MarkEmailAsUnreadAsync(Email email);

	Task MarkEmailAsArchivedAsync(Email email);

	Task RestoreEmailAsync(Email email);

	Task DeleteEmailAsync(Email email);

	Task MarkEmailAsTrashedAsync(Email email);

	Task MarkEmailAsPhishingSpamAsync(Email email);

	Task MarkEmailAsNotPhishingSpamAsync(Email email);

	Task ApplySecurityAnalysisAsync(Email email);
}
