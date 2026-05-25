using System.Threading.Tasks;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Contracts.Services;

public interface IEmailsSyncService
{
	Task StartAsync();

	Task RunAsync();

	Task SyncNewEmailsAsync(AccountMailBase account);

	void Stop();
}
