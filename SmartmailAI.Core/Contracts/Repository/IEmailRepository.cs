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

	// TODO: Mettre en paramètres : (AccountOutlook? accountOutlook)
	Task DeleteAllEmailsAsync(AccountGmail? accountGmail = null, AccountOther? accountOther = null);
}
