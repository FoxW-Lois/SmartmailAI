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

	public Email MapEmailGmailToEmail(EmailGmail emailGmail)
	{
		return new Email
		{
			Guid = emailGmail.Guid,
			SenderEmail = emailGmail.FromEmail,
			SenderName = emailGmail.FromName ?? emailGmail.FromEmail,
			ReceiverEmail = emailGmail.ToEmail,
			ReceiverName = emailGmail.ToName,
			Subject = emailGmail.Subject,
			Content = emailGmail.Body,
			DateSent = emailGmail.Date,
			Owner = emailGmail.Owner,
			Attachments = emailGmail.Attachments,
			MailboxType = emailGmail.MailboxType switch
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

	public async Task<List<Email>> MapEmailGmailToEmail_List(List<EmailGmail> emailGmailList)
	{
		List<Email> emailList = [];
		foreach (var emailGmail in emailGmailList)
		{
			var newEmail = MapEmailGmailToEmail(emailGmail);
			emailList.Add(newEmail);
		}

		await Task.CompletedTask;
		return emailList;
	}
}
