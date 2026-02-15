using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace SmartmailAI.Core.Models;

public class Email
{
	#region Propriétés de base / Composition

	[Key][Column("Id_internal")] public int Id_internal { get; init; } = default!;
	[Column("Guid")] public string Guid { get; init; } = default!;

	[Column("SenderEmail")] public string SenderEmail { get; set; } = default!;
	[Column("SenderName")] public string SenderName { get; set; } = default!; // ← Nullable en bdd mais recevra le SenderEmail si null côté UI
	[NotMapped] public Uri? SenderProfileImage { get; set; }

	[Column("ReceiverEmail")] public string? ReceiverEmail { get; set; }
	[Column("ReceiverName")] public string? ReceiverName { get; set; }
	[NotMapped] public Uri? ReceiverProfileImage { get; set; }

	[Column("Subject")] public string? Subject { get; set; }
	[Column("Content")] public string? Content { get; set; }
	[Column("Owner")] public string Owner { get; set; } = default!;

	[Column("DateSent")] public DateTime? DateSent { get; set; }

	[Column("Attachments")] public List<string>? Attachments { get; set; }

	[NotMapped]
	public string AttachmentsDisplay => Attachments != null && Attachments.Count != 0
		? string.Join(", ", Attachments)
		: "Aucune pièce jointe";

	#endregion Propriétés de base / Composition

	#region Propriétés de gestion de l'état des mails

	// Catégorisation/localisation du mail
	[Column("MailboxType")] public MailboxType MailboxType { get; set; }

	// Précédente catégorisation/localisation avant suppression
	[Column("PreviousMailboxType")] public MailboxType? PreviousMailboxType { get; set; } = null;

	// Statut de favori
	[Column("IsStarred")] public bool IsStarred { get; set; } = false;

	// Statut de lecture
	[Column("IsRead")] public bool IsRead { get; set; } = false;

	#endregion Propriétés de gestion de l'état des mails

	#region Propriétés dédiées à l'affichage

	// Se remplit à partir de SenderProfileImage
	[NotMapped]
	public ImageSource SenderProfileImageSource
	{
		get
		{
			if (SenderProfileImage == null)
				return new BitmapImage(new Uri("ms-appx:///Assets/Content/Default-Avatar-icon.jpg"));

			return new BitmapImage(SenderProfileImage);
		}
	}

	// Se remplit à partir de ReceiverProfileImage
	[NotMapped]
	public ImageSource ReceiverProfileImageSource
	{
		get
		{
			if (ReceiverProfileImage == null)
				return new BitmapImage(new Uri("ms-appx:///Assets/Content/Default-Avatar-icon.jpg"));

			return new BitmapImage(ReceiverProfileImage);
		}
	}

	// Se remplit à partir de Content
	[NotMapped]
	public string? PreviewContent
	{
		get
		{
			if (string.IsNullOrWhiteSpace(Content)) return string.Empty;

			string cleaned = System.Text.RegularExpressions.Regex.Replace(Content, @"\s+", " ").Trim();

			// Prend les 100 premiers caractères
			return cleaned[..Math.Min(100, cleaned.Length)];
		}
	}

	// Se remplissent à partir de DateSent
	[NotMapped] public DateOnly? DaySent => DateSent is null ? null : DateOnly.FromDateTime(DateSent.Value);

	[NotMapped] public TimeOnly? TimeSent => DateSent is null ? null : TimeOnly.FromDateTime(DateSent.Value);

	[NotMapped] public bool IsSameDay => DateSent.HasValue && DateSent.Value.Date == DateTime.Today;

	[NotMapped]
	public string DisplayDateSent
	{
		get
		{
			if (!DateSent.HasValue) return string.Empty;

			return IsSameDay
				? DateSent.Value.ToString("HH:mm")
				: DateSent.Value.ToString("dd/MM/yyyy");
		}
	}

	#endregion Propriétés dédiées à l'affichage
}
