using System.Collections.Generic;
using System.Threading.Tasks;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Contracts.Services;

public interface IMappersToEmailDTOService
{
	Email MapEmailGmailToEmail(EmailGmail emailGmail);

	Task<List<Email>> MapEmailGmailToEmail_List(List<EmailGmail> emailGmailList);
}
