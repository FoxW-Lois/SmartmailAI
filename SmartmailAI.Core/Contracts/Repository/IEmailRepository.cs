using System.Collections.Generic;
using System.Threading.Tasks;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Contracts.Repository;

public interface IEmailRepository
{
	Task<List<Email>> GetAllEmailsAsync();

	Task AddEmailAsync(Email email);

	Task UpdateEmailAsync(Email email);

	Task DeleteEmailAsync(Email email);

	Task DeleteAllEmailsAsync(AccountGmail accountGmail);
}
