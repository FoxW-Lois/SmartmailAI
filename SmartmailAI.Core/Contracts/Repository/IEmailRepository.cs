using System.Collections.Generic;
using System.Threading.Tasks;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Contracts.Repository;

public interface IEmailRepository
{
	Task<List<Email>> GetAllEmailsAsync();

	Task<List<Email>> GetAllEmailsByAddressAsync(string ownerAddress);

	Task AddEmailAsync(Email email);

	Task UpdateEmailAsync(Email email);

	Task DeleteEmailAsync(Email email);

	Task DeleteAllEmailsAsync(AccountMailBase account);

	Task<IReadOnlyList<Email>> KeepOnlyNewEmailsAsync(string ownerAddress, List<Email> newEmails);
}
