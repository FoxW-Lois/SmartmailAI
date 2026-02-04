using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1.Data;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Contracts.Services;

public interface IAddressesService
{
	Task<(AccountGmail, bool)> AddGmailAccountAsync();

	Task<bool> AddOutlookAsync();

	Task<bool> AddOtherAddressAsync();

	Task ListLast50GmailEmailsAsync(UserCredential credential);

	Task<bool> RemoveGmailAccountAsync(AccountGmail account);

	string GetMessageBody(Message message);

	string DecodeBase64(string input);
}
