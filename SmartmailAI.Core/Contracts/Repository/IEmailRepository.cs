using System.Collections.Generic;
using System.Threading.Tasks;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Contracts.Repository;

public interface IEmailRepository
{
	Task<List<EmailGmail>> GetAllEmailsAsync();

	Task AddEmailAsync(EmailGmail emailGmail);

	Task DeleteEmailAsync(EmailGmail emailGmail);

	Task DeleteAllEmailsAsync(AccountGmail accountGmail);
}
