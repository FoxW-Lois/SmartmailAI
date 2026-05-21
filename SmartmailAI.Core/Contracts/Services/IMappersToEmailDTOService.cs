using System.Collections.Generic;
using System.Threading.Tasks;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Contracts.Services;

public interface IMappersToEmailDTOService
{
	Email MapEmailFromAddressToEmail(EmailFromAddress emailFromAddress);

	Task<List<Email>> MapEmailFromAddressToEmail_List(List<EmailFromAddress> emailFromAddressList);
}
