using System.Collections.Generic;
using System.Threading.Tasks;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.IRepository;

public interface IEmailRepository
{
	Task<List<EmailGmail>> GetAllAddressAsync();

	Task AddAsync(EmailGmail emailGmail);

	Task DeleteAsync(EmailGmail emailGmail);
}
