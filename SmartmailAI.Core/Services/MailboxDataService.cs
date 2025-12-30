using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmartmailAI.Core.Contracts.Services;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Services;

public class MailboxDataService : IMailboxDataService
{
	private List<Email> _AllEmails;

	public MailboxDataService()
	{
	}

	public async Task<IEnumerable<MailboxCategory>> GetAllCategoriesAsync()
	{
		_AllEmails ??= [.. AllEmails()];

		var categories = new List<MailboxCategory>
		{
			new MailboxCategory
			{
				Title = "Boîte de réception",
				Icon = "\uE715", // Mail
			    Items = _AllEmails.Where(e => e.MailboxType == MailboxType.Inbox)
			},
			new MailboxCategory
			{
				Title = "Messages envoyés",
				Icon = "\uE122", // Send
			    Items = _AllEmails.Where(e => e.MailboxType == MailboxType.Sent)
			},
			new MailboxCategory
			{
				Title = "Phishing",
				Icon = "\uE7BA", // Warning
			    Items = _AllEmails.Where(e => e.MailboxType == MailboxType.Phishing)
			}
		};

		await Task.CompletedTask;
		return categories;
	}

	private static IEnumerable<Email> AllEmails()
	{
		return [
			new Email
			{
				SenderName = "Jean Dupont",
				SenderEmail = "jean.dupont@exemple.com",
				SenderProfileImage = new Uri("https://randomuser.me/api/portraits/men/32.jpg"),
				ReceiverName = "Marie Martin",
				ReceiverEmails = "marie.martin@exemple.com",
				ReceiverProfileImage = new Uri("https://randomuser.me/api/portraits/women/32.jpg"),
				Subject = "Réunion de suivi",
				Content = "Bonjour Marie,\n\nPeux-tu me confirmer ta disponibilité pour la réunion de suivi prévue demain à 10h ?\n\nCordialement,\nJean",
				PreviewContent = "Bonjour Marie, Peux-tu me confirmer ta disponibilité",
				DateSent = DateTime.Now.AddDays(-2),
				Attachments = [ "Ordre_du_jour.pdf" ],
				MailboxType = MailboxType.Inbox
			},
			new Email
			{
				SenderName = "Service RH",
				SenderEmail = "rh@entreprise.com",
				SenderProfileImage = new Uri("https://randomuser.me/api/portraits/women/32.jpg"),
				ReceiverName = "Paul Durand",
				ReceiverEmails = "paul.durand@entreprise.com",
				ReceiverProfileImage = new Uri("https://randomuser.me/api/portraits/men/32.jpg"),
				Subject = "Confirmation de congés",
				Content = "Bonjour Paul,\n\nTes congés du 12 au 18 août ont bien été validés.\n\nBonne journée,\nService RH",
				PreviewContent = "Bonjour Paul, Tes congés du 12 au 18 août ont bien",
				DateSent = DateTime.Now.AddDays(-7),
				Attachments = [],
				MailboxType = MailboxType.Inbox
			},
			new Email
			{
				SenderName = "Support Technique",
				SenderEmail = "support@logiciel.com",
				SenderProfileImage = new Uri("https://randomuser.me/api/portraits/men/32.jpg"),
				ReceiverName = "Claire Bernard",
				ReceiverEmails = "claire.bernard@client.com; claire.b@client.com",
				ReceiverProfileImage = new Uri("https://randomuser.me/api/portraits/women/32.jpg"),
				Subject = "Ticket #45821 résolu",
				Content = "Bonjour Claire,\n\nNous vous confirmons que le problème signalé a été corrigé.\nN'hésitez pas à nous recontacter en cas de besoin.\n\nCordialement,\nSupport Technique",
				PreviewContent = "Bonjour Claire, Nous vous confirmons que le problème",
				DateSent = DateTime.Now.AddHours(-5),
				Attachments = [ "rapport_intervention.docx", "capture_ecran.png" ],
				MailboxType = MailboxType.Inbox
			}
		];
	}

	public async Task<IEnumerable<Email>> GetListDetails_AllEmailsAsync()
	{
		_AllEmails ??= [.. AllEmails()];

		await Task.CompletedTask;
		return _AllEmails;
	}
}
