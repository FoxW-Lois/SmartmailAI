using System.Collections.Generic;
using System.Threading.Tasks;
using SmartmailAI.Core.Contracts.Services;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Services;

public class MappersToEmailDTOService(IEmailsService emailsService) : IMappersToEmailDTOService
{
	private readonly IEmailsService _emailsService = emailsService;

	public Email MapEmailFromAddressToEmail(EmailFromAddress emailFromAddress)
	{
		return new Email
		{
			Guid = emailFromAddress.Guid,
			SenderEmail = emailFromAddress.FromEmail,
			SenderName = !string.IsNullOrWhiteSpace(emailFromAddress.FromName) ? emailFromAddress.FromName : emailFromAddress.FromEmail,
			ReceiverEmail = emailFromAddress.ToEmail,
			ReceiverName = !string.IsNullOrWhiteSpace(emailFromAddress.ToName) ? emailFromAddress.ToName : emailFromAddress.ToEmail,
			Cc = emailFromAddress.Cc,
			Bcc = emailFromAddress.Bcc,
			Subject = emailFromAddress.Subject,
			Content = emailFromAddress.Body,
			DateSent = emailFromAddress.Date,
			Owner = emailFromAddress.Owner,
			Attachments = emailFromAddress.Attachments,
			MailboxType = emailFromAddress.MailboxType switch
			{
				"Inbox" => MailboxType.Inbox,
				"Sent" => MailboxType.Sent,
				"Draft" => MailboxType.Drafts,
				"Spam" => MailboxType.PhishingSpam,
				"Trash" => MailboxType.Trash,
				_ => MailboxType.Inbox
			},
			PreviousMailboxType = null,
			IsStarred = false,
			IsRead = false
		};
	}

	public async Task<List<Email>> MapEmailFromAddressToEmail_List(List<EmailFromAddress> emailFromAddressList)
	{
		List<Email> emailList = [];
		foreach (var emailFromAddress in emailFromAddressList)
		{
			var newEmail = MapEmailFromAddressToEmail(emailFromAddress);
			await _emailsService.ApplySecurityAnalysisAsync(newEmail);

			emailList.Add(newEmail);
		}

		await Task.CompletedTask;
		return emailList;
	}
}
