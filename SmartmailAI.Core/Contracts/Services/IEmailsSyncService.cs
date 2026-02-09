using System.Collections.Generic;
using System.Threading.Tasks;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Contracts.Services;

public interface IEmailsSyncService
{
	Task StartAsync();


	Task SyncNewEmailsAsync(AccountGmail accountGmail);

	void Stop();
}
