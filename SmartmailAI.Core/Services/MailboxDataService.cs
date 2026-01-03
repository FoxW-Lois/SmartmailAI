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
			    Items = _AllEmails.Where(e => e.MailboxType == MailboxType.Inbox),
				ViewType = MailboxCategoryViewType.Standard
			},
			new MailboxCategory
			{
				Title = _resources.GetString("Mailbox_Sent"),
				Icon = "\uE122", // Send
			    Items = _AllEmails.Where(e => e.MailboxType == MailboxType.Sent),
				ViewType = MailboxCategoryViewType.Standard
			},
			new MailboxCategory
			{
				Title = _resources.GetString("Mailbox_Snoozed"),
				Icon = "\uE823", // Clock
			    Items = _AllEmails.Where(e => e.MailboxType == MailboxType.Snoozed),
				ViewType = MailboxCategoryViewType.Standard
			},
			new MailboxCategory
			{
				Title = _resources.GetString("Mailbox_Drafts"),
				Icon = "\uE7C3", // Document
			    Items = _AllEmails.Where(e => e.MailboxType == MailboxType.Drafts),
				ViewType = MailboxCategoryViewType.Special
			},
			new MailboxCategory
			{
				Title = _resources.GetString("Mailbox_Starred"),
				Icon = "\uE734", // FavoriteStar
			    Items = _AllEmails.Where(e => e.MailboxType == MailboxType.Starred),
				ViewType = MailboxCategoryViewType.Special
			},
			new MailboxCategory
			{
				Title = _resources.GetString("Mailbox_Trash"),
				Icon = "\uE74D", // Delete
			    Items = _AllEmails.Where(e => e.MailboxType == MailboxType.Trash),
				ViewType = MailboxCategoryViewType.Grid
			},
			new MailboxCategory
			{
				Title = _resources.GetString("Mailbox_AllMails"),
				Icon = "\uE8F1", // AllApps
			    Items = _AllEmails.Where(e => e.MailboxType == MailboxType.AllMails),
				ViewType = MailboxCategoryViewType.Standard
			},
			new MailboxCategory
			{
				Title = _resources.GetString("Mailbox_Archives"),
				Icon = "\uE7B8", // Archive
			    Items = _AllEmails.Where(e => e.MailboxType == MailboxType.Archives),
				ViewType = MailboxCategoryViewType.Grid
			},
			new MailboxCategory
			{
				Title = _resources.GetString("Mailbox_PhishingSpam"),
				Icon = "\uE7BA", // Warning
			    Items = _AllEmails.Where(e => e.MailboxType == MailboxType.PhishingSpam),
				ViewType = MailboxCategoryViewType.Grid
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
				DateSent = DateTime.Now.AddHours(-6),
				Attachments = [],
				MailboxType = MailboxType.Sent
			},
			// ------ Snoozed Emails ------
			new Email
			{
				SenderName = "Service Comptabilité",
				SenderEmail = "compta@entreprise.com",
				SenderProfileImage = new Uri("https://randomuser.me/api/portraits/women/60.jpg"),
				ReceiverName = "Jean Dupont",
				ReceiverEmail = "jean.dupont@exemple.com",
				ReceiverProfileImage = new Uri("https://randomuser.me/api/portraits/men/32.jpg"),
				Subject = "Note de frais à valider",
				Content = "Bonjour Jean,\n\nMerci de valider la note de frais du mois dernier avant la fin de semaine.\n\nCordialement,\nComptabilité",
				DateSent = DateTime.Now.AddDays(-3),
				Attachments = [ "note_de_frais.pdf" ],
				MailboxType = MailboxType.Snoozed
			},
			new Email
			{
				SenderName = "Claire Bernard",
				SenderEmail = "claire.bernard@client.com",
				SenderProfileImage = new Uri("https://randomuser.me/api/portraits/women/51.jpg"),
				ReceiverName = "Jean Dupont",
				ReceiverEmail = "jean.dupont@exemple.com",
				ReceiverProfileImage = new Uri("https://randomuser.me/api/portraits/men/32.jpg"),
				Subject = "Retour sur la proposition",
				Content = "Bonjour Jean,\n\nJe reviens vers toi concernant la proposition envoyée la semaine dernière.\n\nÀ bientôt,\nClaire",
				DateSent = DateTime.Now.AddDays(-4),
				Attachments = [],
				MailboxType = MailboxType.Snoozed
			},
			// ------ Drafts Emails ------
			new Email
			{
				SenderName = "Jean Dupont",
				SenderEmail = "jean.dupont@exemple.com",
				SenderProfileImage = new Uri("https://randomuser.me/api/portraits/men/32.jpg"),
				ReceiverProfileImage = null,
				Subject = "Demande de télétravail",
				Content = "Bonjour,\n\nJe souhaiterais discuter de la possibilité de télétravailler un jour par semaine.",
				DateSent = DateTime.Now,
				Attachments = [],
				MailboxType = MailboxType.Drafts
			},
			new Email
			{
				SenderName = "Jean Dupont",
				SenderEmail = "jean.dupont@exemple.com",
				SenderProfileImage = new Uri("https://randomuser.me/api/portraits/men/32.jpg"),
				ReceiverProfileImage = new Uri("https://randomuser.me/api/portraits/women/32.jpg"),
				Subject = "Compte-rendu réunion",
				Content = "Bonjour Marie,\n\nVoici un premier brouillon du compte-rendu de la réunion.",
				DateSent = DateTime.Now,
				Attachments = [ "compte_rendu_draft.docx" ],
				MailboxType = MailboxType.Drafts
			},
			// ------ Starred Emails ------
			new Email
			{
				SenderName = "Direction Générale",
				SenderEmail = "direction@entreprise.com",
				SenderProfileImage = new Uri("https://randomuser.me/api/portraits/men/80.jpg"),
				ReceiverName = "Jean Dupont",
				ReceiverEmail = "jean.dupont@exemple.com",
				ReceiverProfileImage = new Uri("https://randomuser.me/api/portraits/men/32.jpg"),
				Subject = "Annonce stratégique",
				Content = "Bonjour Jean,\n\nUne annonce importante sera faite lors de la réunion de vendredi.\n\nCordialement,\nLa Direction",
				DateSent = DateTime.Now.AddDays(-10),
				Attachments = [],
				MailboxType = MailboxType.Starred
			},
			new Email
			{
				SenderName = "Jean Dupont",
				SenderEmail = "jean.dupont@exemple.com",
				SenderProfileImage = new Uri("https://randomuser.me/api/portraits/men/32.jpg"),
				ReceiverName = "Client Premium",
				ReceiverEmail = "contact@client-premium.com",
				ReceiverProfileImage = null,
				Subject = "Contrat signé",
				Content = "Bonjour,\n\nJe vous confirme que le contrat a bien été signé.\n\nCordialement,\nJean Dupont",
				DateSent = DateTime.Now.AddDays(-15),
				Attachments = [ "contrat_signe.pdf" ],
				MailboxType = MailboxType.Starred
			},
			// ------ Trash Emails ------
			new Email
			{
				SenderName = "Newsletter Tech",
				SenderEmail = "news@tech-mail.com",
				SenderProfileImage = null,
				ReceiverName = "Jean Dupont",
				ReceiverEmail = "jean.dupont@exemple.com",
				ReceiverProfileImage = new Uri("https://randomuser.me/api/portraits/men/32.jpg"),
				Subject = "Les nouveautés de la semaine",
				Content = "Découvrez les dernières tendances technologiques de la semaine.",
				DateSent = DateTime.Now.AddDays(-20),
				Attachments = [],
				MailboxType = MailboxType.Trash
			},
			new Email
			{
				SenderName = "Publicité",
				SenderEmail = "promo@deals-now.com",
				SenderProfileImage = null,
				ReceiverName = "Jean Dupont",
				ReceiverEmail = "jean.dupont@exemple.com",
				ReceiverProfileImage = new Uri("https://randomuser.me/api/portraits/men/32.jpg"),
				Subject = "Offre exclusive limitée",
				Content = "Profitez de cette offre exceptionnelle valable aujourd’hui seulement.",
				DateSent = DateTime.Now.AddDays(-30),
				Attachments = [],
				MailboxType = MailboxType.Trash
			},
			// ------ Archives Emails ------
			new Email
			{
				SenderName = "Ancien Manager",
				SenderEmail = "manager@ancienne-entreprise.com",
				SenderProfileImage = new Uri("https://randomuser.me/api/portraits/men/90.jpg"),
				ReceiverName = "Jean Dupont",
				ReceiverEmail = "jean.dupont@exemple.com",
				ReceiverProfileImage = new Uri("https://randomuser.me/api/portraits/men/32.jpg"),
				Subject = "Fin de mission",
				Content = "Bonjour Jean,\n\nMerci pour ton travail durant cette mission.\n\nBonne continuation.",
				DateSent = DateTime.Now.AddYears(-1),
				Attachments = [],
				MailboxType = MailboxType.Archives
			},
			new Email
			{
				SenderName = "Service Formation",
				SenderEmail = "formation@entreprise.com",
				SenderProfileImage = new Uri("https://randomuser.me/api/portraits/women/70.jpg"),
				ReceiverName = "Jean Dupont",
				ReceiverEmail = "jean.dupont@exemple.com",
				ReceiverProfileImage = new Uri("https://randomuser.me/api/portraits/men/32.jpg"),
				Subject = "Validation formation",
				Content = "Bonjour Jean,\n\nTa formation a bien été validée.\n\nCordialement,\nService Formation",
				DateSent = DateTime.Now.AddMonths(-6),
				Attachments = [ "certificat.pdf" ],
				MailboxType = MailboxType.Archives
			},
			// ------ Phishing & Spam Emails ------
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
				DateSent = DateTime.Now.AddHours(-12),
				Attachments = [],
				MailboxType = MailboxType.PhishingSpam
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
				DateSent = DateTime.Now.AddDays(-1),
				Attachments = [],
				MailboxType = MailboxType.PhishingSpam
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
				DateSent = DateTime.Now.AddDays(-3),
				Attachments = [ "facture.zip" ],
				MailboxType = MailboxType.PhishingSpam
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
