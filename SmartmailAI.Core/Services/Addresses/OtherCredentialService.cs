using System.Threading.Tasks;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using SmartmailAI.Core.Contracts.Services.Addresses;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Services.Addresses;

public class OtherCredentialService : IOtherCredentialService
{
	public async Task<bool> ConnectAsync(AccountOther account)
	{
		try
		{
			// Vérification IMAP
			using var imap = new ImapClient();

			await imap.ConnectAsync(account.ImapHost, account.ImapPort, account.ImapUseSsl);
			await imap.AuthenticateAsync(account.UserName, account.Password);
			await imap.DisconnectAsync(true);

			// Vérification SMTP
			using var smtp = new SmtpClient();

			await smtp.ConnectAsync(account.SmtpHost, account.SmtpPort, account.SmtpUseSsl);
			await smtp.AuthenticateAsync(account.UserName, account.Password);
			await smtp.DisconnectAsync(true);

			return true;
		}
		catch
		{
			return false;
		}
	}
}
