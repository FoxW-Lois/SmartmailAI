using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Windows.ApplicationModel.Resources;
using SmartmailAI.Core.Contracts.Services;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Services;

public class MailboxDataService : IMailboxDataService
{
	private List<Email> _AllEmails;
	private static readonly ResourceLoader _resources = new();

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
				Title = _resources.GetString("Mailbox_Inbox"),
				Icon = "\uE715", // Mail
			    Items = _AllEmails.Where(e => e.MailboxType == MailboxType.Inbox)
			},
			new MailboxCategory
			{
				Title = _resources.GetString("Mailbox_Sent"),
				Icon = "\uE122", // Send
			    Items = _AllEmails.Where(e => e.MailboxType == MailboxType.Sent)
			},
			new MailboxCategory
			{
				Title = _resources.GetString("Mailbox_Phishing"),
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
			// ------ Inbox Emails ------
			new Email
			{
				SenderName = "Marie Martin",
				SenderEmail = "marie.martin@exemple.com",
				SenderProfileImage = new Uri("https://randomuser.me/api/portraits/women/32.jpg"),
				ReceiverName = "Jean Dupont",
				ReceiverEmail = "jean.dupont@exemple.com",
				ReceiverProfileImage = new Uri("https://randomuser.me/api/portraits/men/32.jpg"),
				Subject = "Réunion de suivi",
				Content = "Bonjour Jean,\n\nPeux-tu me confirmer ta disponibilité pour la réunion de suivi prévue demain à 10h ?\n\nCordialement,\nMarie",
				PreviewContent = "Peux-tu me confirmer ta disponibilité pour la réunion",
				DateSent = DateTime.Now.AddDays(-2),
				Attachments = [ "Ordre_du_jour.pdf" ],
				MailboxType = MailboxType.Inbox
			},
			new Email
			{
				SenderName = "Service RH",
				SenderEmail = "rh@entreprise.com",
				SenderProfileImage = new Uri("https://randomuser.me/api/portraits/women/45.jpg"),
				ReceiverName = "Jean Dupont",
				ReceiverEmail = "jean.dupont@exemple.com",
				ReceiverProfileImage = new Uri("https://randomuser.me/api/portraits/men/32.jpg"),
				Subject = "Mise à jour dossier employé",
				Content = "Bonjour Jean,\n\nMerci de vérifier les informations de ton dossier RH via l’intranet.\n\nCordialement,\nService RH",
				PreviewContent = "Merci de vérifier les informations de ton dossier RH",
				DateSent = DateTime.Now.AddDays(-5),
				Attachments = [],
				MailboxType = MailboxType.Inbox
			},
			new Email
			{
				SenderName = "Support Technique",
				SenderEmail = "support@logiciel.com",
				SenderProfileImage = new Uri("https://randomuser.me/api/portraits/men/51.jpg"),
				ReceiverName = "Jean Dupont",
				ReceiverEmail = "jean.dupont@exemple.com",
				ReceiverProfileImage = new Uri("https://randomuser.me/api/portraits/men/32.jpg"),
				Subject = "Maintenance planifiée",
				Content = "Bonjour Jean,\n\nUne maintenance du système est prévue ce soir entre 22h et 23h.\n\nMerci de ta compréhension.",
				PreviewContent = "Une maintenance du système est prévue ce soir",
				DateSent = DateTime.Now.AddHours(-8),
				Attachments = [],
				MailboxType = MailboxType.Inbox
			},
			// ------ Sent Emails ------
			new Email
			{
				SenderName = "Jean Dupont",
				SenderEmail = "jean.dupont@exemple.com",
				SenderProfileImage = new Uri("https://randomuser.me/api/portraits/men/32.jpg"),
				ReceiverName = "Marie Martin",
				ReceiverEmail = "marie.martin@exemple.com",
				ReceiverProfileImage = new Uri("https://randomuser.me/api/portraits/women/32.jpg"),
				Subject = "RE: Réunion de suivi",
				Content = "Bonjour Marie,\n\nC’est confirmé pour demain à 10h.\n\nÀ demain,\nJean",
				PreviewContent = "C’est confirmé pour demain à 10h",
				DateSent = DateTime.Now.AddDays(-1),
				Attachments = [],
				MailboxType = MailboxType.Sent
			},
			new Email
			{
				SenderName = "Jean Dupont",
				SenderEmail = "jean.dupont@exemple.com",
				SenderProfileImage = new Uri("https://randomuser.me/api/portraits/men/32.jpg"),
				ReceiverName = "Service RH",
				ReceiverEmail = "rh@entreprise.com",
				ReceiverProfileImage = new Uri("https://randomuser.me/api/portraits/women/45.jpg"),
				Subject = "RE: Mise à jour dossier employé",
				Content = "Bonjour,\n\nLes informations ont été vérifiées et mises à jour.\n\nCordialement,\nJean Dupont",
				PreviewContent = "Les informations ont été vérifiées et mises à jour",
				DateSent = DateTime.Now.AddDays(-4),
				Attachments = [],
				MailboxType = MailboxType.Sent
			},
			new Email
			{
				SenderName = "Jean Dupont",
				SenderEmail = "jean.dupont@exemple.com",
				SenderProfileImage = new Uri("https://randomuser.me/api/portraits/men/32.jpg"),
				ReceiverName = "Support Technique",
				ReceiverEmail = "support@logiciel.com",
				ReceiverProfileImage = new Uri("https://randomuser.me/api/portraits/men/51.jpg"),
				Subject = "Question maintenance",
				Content = "Bonjour,\n\nCette maintenance aura-t-elle un impact sur l’accès distant ?\n\nMerci,\nJean",
				PreviewContent = "Cette maintenance aura-t-elle un impact sur l’accès distant",
				DateSent = DateTime.Now.AddHours(-6),
				Attachments = [],
				MailboxType = MailboxType.Sent
			},
			// ------ Phishing Emails ------
			new Email
			{
				SenderName = "Sécurité Banque",
				SenderEmail = "alert@banque-securite.info",
				SenderProfileImage = new Uri("https://randomuser.me/api/portraits/men/66.jpg"),
				ReceiverName = "Jean Dupont",
				ReceiverEmail = "jean.dupont@exemple.com",
				ReceiverProfileImage = new Uri("https://randomuser.me/api/portraits/men/32.jpg"),
				Subject = "⚠️ Compte bancaire suspendu",
				Content = "Nous avons détecté une activité inhabituelle sur votre compte.\nMerci de confirmer vos informations sous 24h.",
				PreviewContent = "Nous avons détecté une activité inhabituelle sur votre compte",
				DateSent = DateTime.Now.AddHours(-12),
				Attachments = [],
				MailboxType = MailboxType.Phishing
			},
			new Email
			{
				SenderName = "Microsoft Support",
				SenderEmail = "support@m1crosoft-verification.com",
				SenderProfileImage = new Uri("https://randomuser.me/api/portraits/men/77.jpg"),
				ReceiverName = "Jean Dupont",
				ReceiverEmail = "jean.dupont@exemple.com",
				ReceiverProfileImage = new Uri("https://randomuser.me/api/portraits/men/32.jpg"),
				Subject = "Votre mot de passe expire aujourd’hui",
				Content = "Votre mot de passe Microsoft arrive à expiration.\nCliquez sur le lien ci-dessous pour le renouveler.",
				PreviewContent = "Votre mot de passe Microsoft arrive à expiration",
				DateSent = DateTime.Now.AddDays(-1),
				Attachments = [],
				MailboxType = MailboxType.Phishing
			},
			new Email
			{
				SenderName = "Livraison Express",
				SenderEmail = "contact@livraison-suivi.co",
				SenderProfileImage = new Uri("https://randomuser.me/api/portraits/women/77.jpg"),
				ReceiverName = "Jean Dupont",
				ReceiverEmail = "jean.dupont@exemple.com",
				Subject = "Colis en attente de paiement",
				Content = "Votre colis est en attente de frais de livraison.\nVeuillez régulariser la situation rapidement.",
				PreviewContent = "Votre colis est en attente de frais de livraison",
				DateSent = DateTime.Now.AddDays(-3),
				Attachments = [ "facture.zip" ],
				MailboxType = MailboxType.Phishing
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
