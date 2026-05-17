using System.Collections.Generic;
using System.Threading.Tasks;
using SmartmailAI.Core.Contracts.Services;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Services;

public class MappersToEmailDTOService : IMappersToEmailDTOService
{
	public MappersToEmailDTOService()
	{
	}

	public Email MapEmailFromAddressToEmail(EmailFromAddress emailFromAddress)
	{
		return new Email
		{
			Guid = emailFromAddress.Guid,
			SenderEmail = emailFromAddress.FromEmail,
			SenderName = emailFromAddress.FromName ?? emailFromAddress.FromEmail,
			ReceiverEmail = emailFromAddress.ToEmail,
			ReceiverName = emailFromAddress.ToName,
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
			emailList.Add(newEmail);
		}

		await Task.CompletedTask;
		return emailList;
	}
}
