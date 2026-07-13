using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Windows.ApplicationModel.Resources;
using SmartmailAI.Core.Contracts.Repository;
using SmartmailAI.Core.Contracts.Services;
using SmartmailAI.Core.Contracts.Services.Security;
using SmartmailAI.Core.Models;
using SmartmailAI.Core.Models.Security;

namespace SmartmailAI.Core.Services;

public class EmailsService(IEmailRepository emailRepository, IRedFlagDomainService redFlagDomainService, IVirusTotalService virusTotalService,
	IDnsSecurityService dnsSecurityService, IMLDA_Repository mldaRepository) : IEmailsService
{
	// Pour l'utilisation de données de tests : ne surtout pas initialiser cette liste, que soit à la déclaration ou bien dans le constructeur
	// Elle doit être initialisée uniquement dans GetAllCategoriesAsync en brute pour garantir que l'analyse de sécurité est appliquée
	// à tous les emails hardcodés avant de les retourner
	private List<Email>? _AllEmails;
	private int _TotalEmailsCount;

	private readonly IEmailRepository _emailRepository = emailRepository;
	private static readonly ResourceLoader _resources = new();
	private readonly IRedFlagDomainService _redFlagDomainService = redFlagDomainService;
	private readonly IVirusTotalService _virusTotalService = virusTotalService;
	private readonly IDnsSecurityService _dnsSecurityService = dnsSecurityService;
	private readonly IMLDA_Repository _mldaRepository = mldaRepository;

	// TODO: Bloc à décommenter pour l'utilisation de données statiques ↓
	/*private readonly List<Email> hardcodedEmails = [
	// ------ Inbox Emails ------
	new Email
	{
		Guid = "Email_Hardcoded-" + Guid.NewGuid().ToString(),
		SenderName = "Marie Martin",
		SenderEmail = "marie.martin@exemple.com",
		SenderProfileImage = new Uri("https://randomuser.me/api/portraits/women/32.jpg"),
		ReceiverName = "Jean Dupont, Lize Jean",
		ReceiverEmail = "jean.dupont@exemple.com, lize.jean@exemple.com",
		ReceiverProfileImage = new Uri("https://randomuser.me/api/portraits/men/32.jpg"),
		Cc = "cc@exemple.com, cc2@exemple.com",
		Bcc = "cci@exemple.com, cci2@exemple.com",
		Subject = "Réunion de suivi",
		Content = "Bonjour Jean,\n\nPeux-tu me confirmer ta disponibilité pour la réunion de suivi prévue demain à 10h ?\n\nCordialement,\nMarie",
		DateSent = DateTime.Now.AddDays(-2),
		Attachments = [],
		MailboxType = MailboxType.Inbox,
		IsRead = false,
		IsStarred = true
	},
	new Email
	{
		Guid = "Email_Hardcoded-" + Guid.NewGuid().ToString(),
		SenderName = "Service RH",
		SenderEmail = "rh@entreprise.com",
		SenderProfileImage = new Uri("https://randomuser.me/api/portraits/women/45.jpg"),
		ReceiverName = "Jean Dupont",
		ReceiverEmail = "jean.dupont@exemple.com",
		ReceiverProfileImage = new Uri("https://randomuser.me/api/portraits/men/32.jpg"),
		Cc = "cc@exemple.com",
		Subject = "Mise à jour dossier employé",
		Content = "Bonjour Jean,\n\nMerci de vérifier les informations de ton dossier RH via l’intranet.\n\nCordialement,\nService RH",
		DateSent = DateTime.Now.AddDays(-5),
		Attachments = [],
		MailboxType = MailboxType.Inbox,
		IsRead = false
	},
	new Email
	{
		Guid = "Email_Hardcoded-" + Guid.NewGuid().ToString(),
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
		MailboxType = MailboxType.Inbox,
		IsRead = true
	},
	//------ Sent Emails ------
	new Email
	{
		Guid = "Email_Hardcoded-" + Guid.NewGuid().ToString(),
		SenderName = "Jean Dupont",
		SenderEmail = "jean.dupont@exemple.com",
		SenderProfileImage = new Uri("https://randomuser.me/api/portraits/men/32.jpg"),
		ReceiverName = "Marie Martin",
		ReceiverEmail = "marie.martin@exemple.com",
		ReceiverProfileImage = new Uri("https://randomuser.me/api/portraits/women/32.jpg"),
		Cc = "cc@exemple.com",
		Bcc = "cci@exemple.com",
		Subject = "RE: Réunion de suivi",
		Content = "Bonjour Marie,\n\nC’est confirmé pour demain à 10h.\n\nÀ demain,\nJean",
		DateSent = DateTime.Now.AddDays(-1),
		Attachments = [],
		MailboxType = MailboxType.Sent,
		IsRead = true,
		IsStarred = true
	},
	new Email
	{
		Guid = "Email_Hardcoded-" + Guid.NewGuid().ToString(),
		SenderName = "Jean Dupont",
		SenderEmail = "jean.dupont@exemple.com",
		SenderProfileImage = new Uri("https://randomuser.me/api/portraits/men/32.jpg"),
		ReceiverName = "Service RH",
		ReceiverEmail = "rh@entreprise.com",
		ReceiverProfileImage = new Uri("https://randomuser.me/api/portraits/women/45.jpg"),
		Cc = "cc@exemple.com",
		Subject = "RE: Mise à jour dossier employé",
		Content = "Bonjour,\n\nLes informations ont été vérifiées et mises à jour.\n\nCordialement,\nJean Dupont",
		DateSent = DateTime.Now.AddDays(-4),
		Attachments = [],
		MailboxType = MailboxType.Sent,
		IsRead = true
	},
	new Email
	{
		Guid = "Email_Hardcoded-" + Guid.NewGuid().ToString(),
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
		MailboxType = MailboxType.Sent,
		IsRead = true
	},
	//------ Drafts Emails ------
	new Email
	{
		Guid = "Email_Hardcoded-" + Guid.NewGuid().ToString(),
		SenderName = "Jean Dupont",
		SenderEmail = "jean.dupont@exemple.com",
		SenderProfileImage = new Uri("https://randomuser.me/api/portraits/men/32.jpg"),
		ReceiverProfileImage = null,
		Subject = "Demande de télétravail",
		Content = "Bonjour,\n\nJe souhaiterais discuter de la possibilité de télétravailler un jour par semaine.",
		DateSent = DateTime.Now,
		Attachments = [],
		MailboxType = MailboxType.Drafts,
		IsRead = true
	},
	new Email
	{
		Guid = "Email_Hardcoded-" + Guid.NewGuid().ToString(),
		SenderName = "Jean Dupont",
		SenderEmail = "jean.dupont@exemple.com",
		SenderProfileImage = new Uri("https://randomuser.me/api/portraits/men/32.jpg"),
		ReceiverProfileImage = new Uri("https://randomuser.me/api/portraits/women/32.jpg"),
		Subject = "Compte-rendu réunion",
		Content = "Bonjour Marie,\n\nVoici un premier brouillon du compte-rendu de la réunion.",
		DateSent = DateTime.Now,
		Attachments = [],
		MailboxType = MailboxType.Drafts,
		IsRead = true
	},
	//------ Trash Emails ------
	new Email
	{
		Guid = "Email_Hardcoded-" + Guid.NewGuid().ToString(),
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
		MailboxType = MailboxType.Trash,
		PreviousMailboxType = MailboxType.Inbox,
		IsRead = true
	},
	new Email
	{
		Guid = "Email_Hardcoded-" + Guid.NewGuid().ToString(),
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
		MailboxType = MailboxType.Trash,
		PreviousMailboxType = MailboxType.Inbox,
		IsRead = true
	},
	//------ Archives Emails ------
	new Email
	{
		Guid = "Email_Hardcoded-" + Guid.NewGuid().ToString(),
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
		MailboxType = MailboxType.Archives,
		PreviousMailboxType = MailboxType.Inbox,
		IsRead = true
	},
	new Email
	{
		Guid = "Email_Hardcoded-" + Guid.NewGuid().ToString(),
		SenderName = "Service Formation",
		SenderEmail = "formation@entreprise.com",
		SenderProfileImage = new Uri("https://randomuser.me/api/portraits/women/70.jpg"),
		ReceiverName = "Jean Dupont",
		ReceiverEmail = "jean.dupont@exemple.com",
		ReceiverProfileImage = new Uri("https://randomuser.me/api/portraits/men/32.jpg"),
		Subject = "Validation formation",
		Content = "Bonjour Jean,\n\nTa formation a bien été validée.\n\nCordialement,\nService Formation",
		DateSent = DateTime.Now.AddMonths(-6),
		Attachments = [],
		MailboxType = MailboxType.Archives,
		PreviousMailboxType = MailboxType.Inbox,
		IsRead = true
	},
	//------ Phishing & Spam Emails (détectés par ApplySecurityAnalysisAsync(_AllEmails)) ------
	new Email
	{
		Guid = "Email_Hardcoded-" + Guid.NewGuid().ToString(),
		SenderName = "Vite ! - Sécurité Banque",
		SenderEmail = "alert@banque-securite.info",
		SenderProfileImage = new Uri("https://randomuser.me/api/portraits/men/66.jpg"),
		ReceiverName = "Jean Dupont",
		ReceiverEmail = "jean.dupont@exemple.com",
		ReceiverProfileImage = new Uri("https://randomuser.me/api/portraits/men/32.jpg"),
		Subject = "⚠️ Compte bancaire suspendu",
		Content = "Nous avons détecté une activité inhabituelle sur votre compte.\nMerci de confirmer vos informations sous 24h.",
		DateSent = DateTime.Now.AddHours(-12),
		Attachments = [],
		MailboxType = MailboxType.Inbox,
		IsRead = false
	},
	new Email
	{
		Guid = "Email_Hardcoded-" + Guid.NewGuid().ToString(),
		SenderName = "Vite ! - Microsoft Support",
		SenderEmail = "support@m1crosoft-verification.com",
		SenderProfileImage = new Uri("https://randomuser.me/api/portraits/men/77.jpg"),
		ReceiverName = "Jean Dupont",
		ReceiverEmail = "jean.dupont@exemple.com",
		ReceiverProfileImage = new Uri("https://randomuser.me/api/portraits/men/32.jpg"),
		Subject = "Votre mot de passe expire aujourd’hui",
		Content = "Votre mot de passe Microsoft arrive à expiration.\nCliquez sur le lien ci-dessous pour le renouveler.",
		DateSent = DateTime.Now.AddDays(-1),
		Attachments = [],
		MailboxType = MailboxType.Inbox,
		IsRead = false
	},
	new Email
	{
		Guid = "Email_Hardcoded-" + Guid.NewGuid().ToString(),
		SenderName = "Vite ! - Livraison Express",
		SenderEmail = "contact@suivi-livraison.fr",
		SenderProfileImage = new Uri("https://randomuser.me/api/portraits/women/77.jpg"),
		ReceiverName = "Jean Dupont",
		ReceiverEmail = "jean.dupont@exemple.com",
		Subject = "Colis en attente de livraison",
		Content = "Votre colis est en attente de livraison.\nVeuillez régulariser la situation dans les plus brefs délais.",
		DateSent = DateTime.Now.AddDays(-3),
		Attachments = [],
		MailboxType = MailboxType.Inbox,
		IsRead = false
	},
	// --- Emails ajoutés via la branche Phishing ---
	new Email
	{
		Guid = "Email_Hardcoded-" + Guid.NewGuid().ToString(),
		SenderName = "Orange Sécurité",
		SenderEmail = "support@lorange.fr",
		ReceiverName = "Jean Dupont",
		ReceiverEmail = "jean.dupont@exemple.com",
		Subject = "Problème de sécurité",
		Content = "Veuillez vérifier votre compte immédiatement.",
		DateSent = DateTime.Now.AddMinutes(-5),
		Attachments = [],
		MailboxType = MailboxType.Inbox,
		IsRead = false
	},
	new Email
	{
		Guid = "Email_Hardcoded-" + Guid.NewGuid().ToString(),
		SenderName = "Microsoft Support",
		SenderEmail = "support@m1crosoft.com",
		ReceiverName = "Jean Dupont",
		ReceiverEmail = "jean.dupont@exemple.com",
		Subject = "Votre mot de passe expire aujourd’hui",
		Content = "Cliquez sur le lien pour renouveler votre mot de passe.",
		DateSent = DateTime.Now.AddMinutes(-10),
		Attachments = [],
		MailboxType = MailboxType.Inbox,
		IsRead = false
	},
	new Email
	{
		Guid = "Email_Hardcoded-" + Guid.NewGuid().ToString(),
		SenderName = "Livraison Express",
		SenderEmail = "contact@livraison-suivi.com",
		ReceiverName = "Jean Dupont",
		ReceiverEmail = "jean.dupont@exemple.com",
		Subject = "Colis bloqué",
		Content = "Paiement requis via bit.ly/livraison pour débloquer votre colis.",
		DateSent = DateTime.Now.AddMinutes(-15),
		Attachments = [],
		MailboxType = MailboxType.Inbox,
		IsRead = false
	},
	new Email
	{
		Guid = "Email_Hardcoded-" + Guid.NewGuid().ToString(),
		SenderName = "Livraison Express",
		SenderEmail = "contact@livraison-suivi.co",
		SenderProfileImage = new Uri("https://randomuser.me/api/portraits/women/77.jpg"),
		ReceiverName = "Jean Dupont",
		ReceiverEmail = "jean.dupont@exemple.com",
		Subject = "Colis en attente de paiement",
		Content = "Votre colis est en attente de frais de livraison.\nVeuillez régulariser la situation rapidement.",
		DateSent = DateTime.Now.AddDays(-3),
		Attachments = [],
		MailboxType = MailboxType.Inbox,
		IsRead = false
	}
];*/

	public async Task<IEnumerable<MailboxCategory>> GetAllCategoriesAsync()
	{
		var categories = new List<MailboxCategory>
		{
			new() {
				Title = _resources.GetString("Mailbox_Inbox"),
				Icon = "\uE715", // Mail
				MailboxType = MailboxType.Inbox
			},
			new() {
				Title = _resources.GetString("Mailbox_Sent"),
				Icon = "\uE122", // Send
				MailboxType = MailboxType.Sent
			},
			new() {
				Title = _resources.GetString("Mailbox_Drafts"),
				Icon = "\uE7C3", // Document
				MailboxType = MailboxType.Drafts
			},
			new() {
				Title = _resources.GetString("Mailbox_Starred"),
				Icon = "\uE734", // FavoriteStar
				MailboxType = MailboxType.Starred
			},
			new() {
				Title = _resources.GetString("Mailbox_Unread"),
				Icon = "\uE8A8", // MailFill
				MailboxType = MailboxType.Unread
			},
			new() {
				Title = _resources.GetString("Mailbox_Trash"),
				Icon = "\uE74D", // Delete
				MailboxType = MailboxType.Trash
			},
			new() {
				Title = _resources.GetString("Mailbox_AllMails"),
				Icon = "\uE8F1", // AllApps
				MailboxType = MailboxType.AllMails
			},
			new() {
				Title = _resources.GetString("Mailbox_Archives"),
				Icon = "\uE7B8", // Archive
				MailboxType = MailboxType.Archives
			},
			new() {
				Title = _resources.GetString("Mailbox_PhishingSpam"),
				Icon = "\uE7BA", // Warning
				MailboxType = MailboxType.PhishingSpam
			}
		};

		return categories;
	}

	public async Task<(IEnumerable<Email>, int totalCount)> GetMailboxEmailsAsync(MailboxType mailboxType, string? addressAccount,
		int page, int pageSize)
	{
		if (addressAccount is not null)
			(_AllEmails, _TotalEmailsCount) = await _emailRepository.GetEmailsByAddressAndMailboxTypeAsync(mailboxType, addressAccount, page, pageSize);
		else
			_AllEmails = [];

		return (_AllEmails, _TotalEmailsCount);

		// TODO: Si besoin d'utiliser des données statiques, commenter ces 5 lignes ↑ et décommenter ces 23 lignes la ↓
		/*_AllEmails = hardcodedEmails;

		foreach (var email in _AllEmails)
			await ApplySecurityAnalysisAsync(email);

		var emails = mailboxType switch
		{
			MailboxType.Inbox => _AllEmails.Where(e => e.MailboxType == MailboxType.Inbox),
			MailboxType.Sent => _AllEmails.Where(e => e.MailboxType == MailboxType.Sent || e.SenderEmail == e.ReceiverEmail),
			MailboxType.Drafts => _AllEmails.Where(e => e.MailboxType == MailboxType.Drafts),
			MailboxType.Starred => _AllEmails.Where(e => e.IsStarred == true),
			MailboxType.Unread => _AllEmails.Where(e => e.IsRead == false && e.MailboxType != MailboxType.Trash
				&& e.MailboxType != MailboxType.Archives && e.MailboxType != MailboxType.PhishingSpam),

			MailboxType.Trash => _AllEmails.Where(e => e.MailboxType == MailboxType.Trash),
			MailboxType.Archives => _AllEmails.Where(e => e.MailboxType == MailboxType.Archives),
			MailboxType.PhishingSpam => _AllEmails.Where(e => e.MailboxType == MailboxType.PhishingSpam),

			_ => _AllEmails.Where(e => e.MailboxType != MailboxType.Drafts && e.MailboxType != MailboxType.Trash
				&& e.MailboxType != MailboxType.Archives && e.MailboxType != MailboxType.PhishingSpam)
		};

		return (emails, _TotalEmailsCount);*/
	}

	public async Task ScribbleEmailAsync(string? guid, string from, string? to, string? subject, string? body, string? cc, string? bcc)
	{
		Email email = new()
		{
			Guid = guid ?? Guid.NewGuid().ToString(),
			SenderEmail = from,
			SenderName = from,
			ReceiverEmail = to,
			ReceiverName = to,
			Subject = subject,
			Content = body,
			Owner = from,
			Cc = cc,
			Bcc = bcc,
			MailboxType = MailboxType.Drafts,
			IsRead = true
		};

		if (guid is null)
			await _emailRepository.AddEmailAsync(email);
		else
			await _emailRepository.UpdateEmailAsync(email);
	}

	#region	Changement d'états des emails

	public async Task MarkEmailAsStarredAsync(Email email)
	{
		if (email is null)
			return;

		email.IsStarred = !email.IsStarred;

		if (email.Guid.StartsWith("Email_Hardcoded-"))
			return;

		await _emailRepository.UpdateEmailAsync(email);

		return;
	}

	public async Task MarkEmailAsReadAsync(Email email)
	{
		if (email is null)
			return;

		email.IsRead = true;

		if (email.Guid.StartsWith("Email_Hardcoded-"))
			return;

		await _emailRepository.UpdateEmailAsync(email);

		return;
	}

	public async Task MarkEmailAsUnreadAsync(Email email)
	{
		if (email is null)
			return;

		email.IsRead = false;

		if (email.Guid.StartsWith("Email_Hardcoded-"))
			return;

		await _emailRepository.UpdateEmailAsync(email);

		return;
	}

	public async Task MarkEmailAsArchivedAsync(Email email)
	{
		if (email is null)
			return;

		email.PreviousMailboxType = email.MailboxType;
		email.MailboxType = MailboxType.Archives;

		if (email.Guid.StartsWith("Email_Hardcoded-"))
			return;

		await _emailRepository.UpdateEmailAsync(email);

		return;
	}

	public async Task RestoreEmailAsync(Email email)
	{
		if (email is null || email.PreviousMailboxType is null)
			return;

		email.MailboxType = (MailboxType)email.PreviousMailboxType;

		if (email.Guid.StartsWith("Email_Hardcoded-"))
			return;

		await _emailRepository.UpdateEmailAsync(email);

		return;
	}

	public async Task DeleteEmailAsync(Email email)
	{
		if (email is null || _AllEmails is null)
			return;

		_AllEmails.Remove(email);

		if (email.Guid.StartsWith("Email_Hardcoded-"))
			return;

		await _emailRepository.DeleteEmailAsync(email);

		return;
	}

	public async Task MarkEmailAsTrashedAsync(Email email)
	{
		if (email is null)
			return;

		if (email.MailboxType is not MailboxType.Archives)
			email.PreviousMailboxType = email.MailboxType;
		
		email.MailboxType = MailboxType.Trash;

		if (email.Guid.StartsWith("Email_Hardcoded-"))
			return;

		await _emailRepository.UpdateEmailAsync(email);

		return;
	}

	public async Task MarkEmailAsPhishingSpamAsync(Email email)
	{
		if (email is null)
			return;

		email.PreviousMailboxType = email.MailboxType;
		email.MailboxType = MailboxType.PhishingSpam;

		if (email.Guid.StartsWith("Email_Hardcoded-"))
			return;

		await _emailRepository.UpdateEmailAsync(email);

		return;
	}

	public async Task MarkEmailAsNotPhishingSpamAsync(Email email)
	{
		if (email is null)
			return;

		email.MailboxType = MailboxType.Inbox;

		if (email.Guid.StartsWith("Email_Hardcoded-"))
			return;

		await _emailRepository.UpdateEmailAsync(email);

		return;
	}

	#endregion Changement d'états des emails

	#region Analyse de sécurité des emails

	public async Task ApplySecurityAnalysisAsync(Email email)
	{
		if (email.MailboxType == MailboxType.Drafts || email.MailboxType == MailboxType.Trash || email.MailboxType == MailboxType.Sent)
			return;

		var reasons = new List<string>();
		int riskScore = await CalculateRiskScoreAsync(email, reasons);

		email.PhishingScore = riskScore;
		email.IsPhishingDetected = riskScore >= 50;
		email.SecurityWarning = GetSecurityWarning(riskScore);
		email.SecurityReasons = reasons.Count > 0
			? string.Join("\n• ", new[] { reasons[0] }.Concat(reasons.Skip(1)))
			: "Aucun signal suspect détecté.";

		var links = ExtractLinks(email.Content);
		email.DetectedLinks = links.Count > 0
			? string.Join("\n", links)
			: "Aucun lien détecté";

		if (riskScore >= 50 && email.MailboxType != MailboxType.PhishingSpam)
		{
			email.PreviousMailboxType = email.MailboxType;
			email.MailboxType = MailboxType.PhishingSpam;
		}
	}

	private static string GetSecurityWarning(int score)
	{
		if (score >= 50)
			return "Phishing probable";
		if (score >= 30)
			return "Email suspect";
		return "Email fiable";
	}

	private async Task<int> CalculateRiskScoreAsync(Email email, List<string> reasons)
	{
		int score = 0;

		// 1. Domaine expéditeur suspect (RedFlagDomains + Levenshtein)
		var (isSuspicious, bypass) = await IsSuspiciousEmailAsync(email.SenderEmail);
		if (isSuspicious)
		{
			score += 40;
			reasons.Add("Domaine expéditeur suspect ou imitant un domaine connu.");
		}

		// 2. Usurpation du nom d'affichage
		if (!bypass && IsDisplayNameSpoofing(email.SenderName, email.SenderEmail, out var spoofedBrand))
		{
			score += 35;
			reasons.Add($"Usurpation d'identité détectée : le nom '{email.SenderName}' imite '{spoofedBrand}' mais le domaine expéditeur ne correspond pas.");
		}

		// 3. Liens suspects dans le corps
		if (ContainsSuspiciousLinks(email.Content, out var suspiciousLinks))
		{
			score += 30;
			reasons.Add($"Lien(s) suspect(s) détecté(s) : {string.Join(", ", suspiciousLinks)}");
		}

		// 4. Pièces jointes - analyse extension + VirusTotal
		if (email.Attachments is { Count: > 0 })
		{
			if (HasDangerousAttachment(email.Attachments))
			{
				score += 20;
				reasons.Add("Pièce jointe avec extension potentiellement dangereuse détectée.");
			}

			var vtResults = await _virusTotalService.AnalyzeAttachmentsAsync(email.Attachments);
			foreach (var vt in vtResults.Where(r => r.IsMalicious))
			{
				// Déjà compté par HasDangerousAttachment → on ajoute seulement si VirusTotal confirme
				if (vt.MaliciousCount > 0)
				{
					score += 15;
					var detail = vt.MaliciousCount > 0
						? $"{vt.MaliciousCount}/{vt.TotalEngines} moteurs"
						: "extension dangereuse connue";
					reasons.Add($"Pièce jointe '{vt.FileName}' signalée comme malveillante ({detail}).");
				}
			}
		}

		// 4. Patterns psychologiques d'urgence (score pondéré 0-20)
		int keywordScore = ScorePhishingKeywords(email.Subject, email.Content);
		System.Diagnostics.Debug.WriteLine($"[Keywords] Score={keywordScore} pour sujet='{email.Subject}'");
		if (keywordScore > 0)
		{
			score += keywordScore;
			string intensity = keywordScore >= 20 ? "critiques" : keywordScore >= 10 ? "élevés" : "faibles";
			reasons.Add($"Formulations d'urgence psychologique détectées (niveau {intensity}).");
		}

		// 5. Vérification DNS (SPF / DMARC)
		// On skip les domaines internes/de test connus pour éviter les faux positifs
		string[] skipDnsDomains = ["exemple.com", "entreprise.com", "client.com", "ancienne-entreprise.com"];
		var senderDomain = email.SenderEmail.Contains('@') == true
			? email.SenderEmail.Split('@')[1].ToLowerInvariant()
			: string.Empty;

		if (!skipDnsDomains.Contains(senderDomain))
		{
			var dns = await _dnsSecurityService.CheckDomainAsync(email.SenderEmail);
			email.SpfStatus = dns.SpfStatus.ToString();
			email.DmarcStatus = dns.DmarcStatus.ToString();
			email.DnsWarning = dns.Warning;

			if (dns.SpfStatus == SpfStatus.None && dns.DmarcStatus == DmarcStatus.None)
			{
				score += 25;
				reasons.Add($"Aucune politique SPF ni DMARC sur '{dns.Domain}' — domaine non authentifié.");
			}
			else if (dns.SpfStatus == SpfStatus.None)
			{
				score += 15;
				reasons.Add($"Aucun enregistrement SPF sur '{dns.Domain}'.");
			}
			else if (dns.DmarcStatus == DmarcStatus.None)
			{
				score += 10;
				reasons.Add($"Aucune politique DMARC sur '{dns.Domain}'.");
			}
			else if (dns.SpfStatus == SpfStatus.SoftFail)
			{
				score += 5;
				reasons.Add($"SPF en mode permissif (~all) sur '{dns.Domain}'.");
			}
		}

		return score;
	}

	private async Task<(bool isSuspicious, bool bypassIsDisplayNameSpoofing)> IsSuspiciousEmailAsync(string senderEmail)
	{
		if (string.IsNullOrWhiteSpace(senderEmail) || !senderEmail.Contains('@'))
			return (false, false);

		senderEmail = senderEmail.Trim().ToLowerInvariant();

		var parts = senderEmail.Split('@');
		if (parts.Length != 2)
			return (false, false);

		var domain = parts[1];

		var mldaList = await _mldaRepository.GetAllMLDA_Async() ?? [];

		var whitelistDomains = mldaList
			.Where(x => x.IsDomain && x.IsWhitelist)
			.Select(x => x.Value.ToLowerInvariant())
			.ToHashSet();

		var whitelistAddresses = mldaList
			.Where(x => !x.IsDomain && x.IsWhitelist)
			.Select(x => x.Value.ToLowerInvariant())
			.ToHashSet();

		var blacklistDomains = mldaList
			.Where(x => x.IsDomain && !x.IsWhitelist)
			.Select(x => x.Value.ToLowerInvariant())
			.ToHashSet();

		var blacklistAddresses = mldaList
			.Where(x => !x.IsDomain && !x.IsWhitelist)
			.Select(x => x.Value.ToLowerInvariant())
			.ToHashSet();

		// Blacklist -> suspect
		if (blacklistDomains.Contains(domain) || blacklistAddresses.Contains(senderEmail))
		{
			return (true, false);
		}

		// Whitelist -> bypass
		if (whitelistDomains.Contains(domain) || whitelistAddresses.Contains(senderEmail))
		{
			return (false, true);
		}

		// Vérification dans la liste de RedFlagDomains
		if (await _redFlagDomainService.IsFlaggedDomainAsync(domain))
		{
			return (true, false);
		}

		// Détection par similarité (TypoSquatting)
		foreach (var legitDomain in whitelistDomains)
		{
			if (AreDomainsSimilar(domain, legitDomain))
				return (true, false);
		}

		return (false, false);
	}

	private static bool AreDomainsSimilar(string a, string b)
	{
		a = a.ToLowerInvariant();
		b = b.ToLowerInvariant();

		if (Math.Abs(a.Length - b.Length) > 3)
			return false;

		int distance = LevenshteinDistance(a, b);
		return distance <= 2;
	}

	private static int LevenshteinDistance(string s, string t)
	{
		if (string.IsNullOrEmpty(s))
			return t?.Length ?? 0;

		if (string.IsNullOrEmpty(t))
			return s.Length;

		int n = s.Length;
		int m = t.Length;
		var d = new int[n + 1, m + 1];

		for (int i = 0; i <= n; i++)
			d[i, 0] = i;

		for (int j = 0; j <= m; j++)
			d[0, j] = j;

		for (int i = 1; i <= n; i++)
		{
			for (int j = 1; j <= m; j++)
			{
				int cost = s[i - 1] == t[j - 1] ? 0 : 1;

				d[i, j] = Math.Min(
					Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
					d[i - 1, j - 1] + cost
				);
			}
		}

		return d[n, m];
	}

	private static bool ContainsSuspiciousLinks(string? content, out List<string> suspiciousLinks)
	{
		suspiciousLinks = [];

		if (string.IsNullOrWhiteSpace(content))
			return false;

		var links = ExtractLinks(content);

		if (links.Count == 0)
			return false;

		string[] suspiciousIndicators =
		[
			"http://",
			"bit.ly",
			"tinyurl.com",
			".xyz",
			".top",
			".click",
			".ru"
		];

		foreach (var link in links)
		{
			string lowerLink = link.ToLowerInvariant();

			if (suspiciousIndicators.Any(indicator => lowerLink.Contains(indicator)))
			{
				suspiciousLinks.Add(link);
				continue;
			}

			if (Uri.TryCreate(link, UriKind.Absolute, out var uri))
			{
				var host = uri.Host.ToLowerInvariant();

				if (ContainsNonAsciiCharacters(host))
				{
					suspiciousLinks.Add(link);
					continue;
				}
			}
		}

		return suspiciousLinks.Count > 0;
	}

	private static List<string> ExtractLinks(string? content)
	{
		var results = new List<string>();

		if (string.IsNullOrWhiteSpace(content))
			return results;

		var parts = content
			.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries)
			.Select(p => p.Trim(',', '.', ';', '!', '?', '(', ')', '[', ']', '"', '\''));

		foreach (var part in parts)
		{
			if (part.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
				part.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
				part.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ||
				part.Contains("bit.ly", StringComparison.OrdinalIgnoreCase) ||
				part.Contains("tinyurl.com", StringComparison.OrdinalIgnoreCase))
			{
				results.Add(part);
			}
		}

		return [.. results.Distinct(StringComparer.OrdinalIgnoreCase)];
	}

	private static bool ContainsNonAsciiCharacters(string text)
	{
		return text.Any(c => c > 127);
	}

	// Détecte l'usurpation du nom d'affichage (Display Name Spoofing).
	// Ex : SenderName = "Apple Support" mais SenderEmail = "contact@random-domain.com"
	private static bool IsDisplayNameSpoofing(string? senderName, string? senderEmail, out string spoofedBrand)
	{
		spoofedBrand = string.Empty;

		if (string.IsNullOrWhiteSpace(senderName) || string.IsNullOrWhiteSpace(senderEmail))
			return false;

		if (!senderEmail.Contains('@'))
			return false;

		var domain = senderEmail.Split('@')[1].Trim().ToLowerInvariant();
		var name = senderName.ToLowerInvariant();

		// Dictionnaire : mot-clé dans le nom d'affichage → domaines officiels autorisés
		var brandMap = new Dictionary<string, string[]>
		{
			["apple"] = ["apple.com", "icloud.com"],
			["microsoft"] = ["microsoft.com", "outlook.com", "live.com", "hotmail.com", "office.com"],
			["google"] = ["google.com", "gmail.com", "googlemail.com"],
			["amazon"] = ["amazon.com", "amazon.fr", "amazon.co.uk", "amazonaws.com"],
			["paypal"] = ["paypal.com", "paypal.fr"],
			["netflix"] = ["netflix.com"],
			["orange"] = ["orange.fr", "orange.com"],
			["free"] = ["free.fr", "freebox.fr"],
			["sfr"] = ["sfr.fr", "sfr.com"],
			["bouygues"] = ["bouyguestelecom.fr", "bbox.fr"],
			["laposte"] = ["laposte.net", "laposte.fr"],
			["impots"] = ["impots.gouv.fr", "dgfip.finances.gouv.fr"],
			["ameli"] = ["ameli.fr", "assurance-maladie.fr"],
			["caf"] = ["caf.fr"],
			["banque"] = ["credit-agricole.fr", "bnpparibas.fr", "societegenerale.fr", "lcl.fr", "labanquepostale.fr"],
			["credit agricole"] = ["credit-agricole.fr", "ca-*.fr"],
			["bnp"] = ["bnpparibas.fr", "bnpparibas.com"],
			["société générale"] = ["societegenerale.fr"],
			["ebay"] = ["ebay.fr", "ebay.com"],
			["leboncoin"] = ["leboncoin.fr"],
			["facebook"] = ["facebook.com", "fb.com", "meta.com"],
			["instagram"] = ["instagram.com", "fb.com"],
			["linkedin"] = ["linkedin.com", "e.linkedin.com"],
			["twitter"] = ["twitter.com", "x.com"],
			["dhl"] = ["dhl.com", "dhl.fr"],
			["chronopost"] = ["chronopost.fr"],
			["colissimo"] = ["colissimo.fr", "laposte.fr"],
		};

		foreach (var (keyword, officialDomains) in brandMap)
		{
			// Le nom d'affichage contient le mot-clé de la marque
			if (!name.Contains(keyword))
				continue;

			System.Diagnostics.Debug.WriteLine($"[DisplayNameSpoofing] 🔍 Keyword '{keyword}' trouvé dans '{senderName}', domaine='{domain}'");

			// Le domaine expéditeur est-il un domaine officiel de cette marque ?
			bool isOfficial = officialDomains.Any(od =>
			{
				// Support du wildcard simple (ex: "ca-*.fr")
				if (od.Contains('*'))
				{
					var prefix = od[..od.IndexOf('*')];
					var suffix = od[(od.IndexOf('*') + 1)..];
					return domain.StartsWith(prefix) && domain.EndsWith(suffix);
				}
				return domain == od;
			});

			System.Diagnostics.Debug.WriteLine($"[DisplayNameSpoofing] isOfficial={isOfficial}");

			if (!isOfficial)
			{
				// Met le nom de la marque en majuscule pour le message
				spoofedBrand = char.ToUpper(keyword[0]) + keyword[1..];
				System.Diagnostics.Debug.WriteLine($"[DisplayNameSpoofing] ⚠️ '{senderName}' <{senderEmail}> → usurpe '{spoofedBrand}'");
				return true;
			}

			// Domaine officiel trouvé → pas d'usurpation
			return false;
		}

		return false;
	}

	private static bool HasDangerousAttachment(List<MailAttachment>? attachments)
	{
		if (attachments is null || attachments.Count == 0)
			return false;

		string[] dangerousExtensions =
		[
			".exe", ".scr", ".bat", ".cmd", ".com", ".pif",
			".js",  ".jse", ".vbs", ".vbe", ".wsf", ".wsh",
			".ps1", ".psm1", ".msi", ".dll", ".sys",
			".zip", ".rar", ".7z", ".iso", ".img",
			".docm", ".xlsm", ".pptm",
			".lnk", ".url",
			".hta", ".htm", ".html"
		];

		return attachments.Any(a => !string.IsNullOrWhiteSpace(a?.FileName) &&
			dangerousExtensions.Any(ext => a.FileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)));
	}

	// Retourne un score de 0 à 20 basé sur l'intensité des patterns psychologiques détectés.
	private static int ScorePhishingKeywords(string? subject, string? content)
	{
		string text = $"{subject} {content}".ToLowerInvariant();

		// Niveau 3 — Urgence extrême (+20)
		string[] criticalPatterns =
		[
			"votre compte sera suspendu",
			"compte suspendu",
			"compte bancaire suspendu",
			"accès bloqué",
			"acces bloque",
			"action requise immédiatement",
			"action requise immediatement",
			"a été compromis",				// Matche avec par exemple "votre compte Apple a été compromis"
			"a ete compromis",
			"compte compromis",
			"activité suspecte détectée",
			"activite suspecte detectee",
			"activité suspecte",
			"activite suspecte",
			"sécurité de votre compte",
			"securite de votre compte",
			"suspendu",						// Capture tous les cas de suspension
			"compromis",					// Capture tous les cas de compromission
			"bloqué",
			"bloque",
		];

		// Niveau 2 — Urgence modérée (+10)
		string[] highPatterns =
		[
			"vérifiez votre compte",
			"verifiez votre compte",
			"confirmez vos informations",
			"mettez à jour vos informations",
			"mettez a jour vos informations",
			"mot de passe expiré",
			"mot de passe expire",
			"expire aujourd'hui",
			"expire aujourd",
			"réinitialisez votre mot de passe",
			"reinitialiser votre mot de passe",
			"cliquez ici pour confirmer",
			"cliquez sur le lien",
			"paiement requis",
			"paiement en attente",
			"facture en attente",
			"colis en attente",
			"colis bloqué",
			"colis bloque",
			"48 heures",
			"24 heures",
			"dans les plus brefs délais",
			"dans les plus brefs delais",
		];

		// Niveau 1 — Signaux faibles (+5)
		string[] lowPatterns =
		[
			"urgent",
			"immédiatement",
			"immediatement",
			"ne pas ignorer",
			"dernière chance",
			"derniere chance",
			"offre limitée",
			"offre limitee",
			"gagnant",
			"félicitations",
			"felicitations",
			"vous avez été sélectionné",
			"vous avez ete selectionne",
			"gratuit",
			"cliquez ici",
			"connectez-vous maintenant",
			"vérification nécessaire",
			"verification necessaire",
			"problème de sécurité",
			"probleme de securite",
			"sécurité",
			"securite",
		];

		int score = 0;

		// Scoring cumulatif : chaque niveau peut s'additionner
		if (criticalPatterns.Any(k => text.Contains(k)))
			score += 20;

		if (highPatterns.Any(k => text.Contains(k)))
			score += 10;

		if (lowPatterns.Any(k => text.Contains(k)))
			score += 5;

		return score;
	}

	#endregion Analyse de sécurité des emails
}
