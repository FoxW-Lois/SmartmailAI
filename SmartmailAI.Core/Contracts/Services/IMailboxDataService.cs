using System.Collections.Generic;
using System.Threading.Tasks;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Contracts.Services;

public interface IMailboxDataService
{
	Task<IEnumerable<MailboxCategory>> GetAllCategoriesAsync();

	Task<IEnumerable<Email>> GetListDetails_AllEmailsAsync();
}
