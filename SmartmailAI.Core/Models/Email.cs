using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace SmartmailAI.Core.Models;

public class Email
{
	// --- Propriétés de base / Composition ---
	public string SenderName { get; set; }
	public string SenderEmail { get; set; }
	public Uri? SenderProfileImage { get; set; }

	public string? ReceiverName { get; set; }
	public string? ReceiverEmail { get; set; }
	public Uri? ReceiverProfileImage { get; set; }

	public string? Subject { get; set; }
	public string? Content { get; set; }

	public DateTime? DateSent { get; set; }

	public List<string>? Attachments { get; set; }

	public string AttachmentsDisplay => Attachments != null && Attachments.Count != 0
		? string.Join(", ", Attachments) 
		: "Aucune pièce jointe";

	// Pour la catégorisation
	public MailboxType MailboxType { get; set; }

	// Statut de favori
	public bool IsStarred { get; set; } = false;

	// Statut de lecture
	public bool IsRead { get; set; } = false;

	// --- Propriété seulement pour l'affichage ---
	// Se remplit à partir de SenderProfileImage
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
	public string? PreviewContent
	{
		get
		{
			if (string.IsNullOrWhiteSpace(Content)) return string.Empty;

			string cleaned = System.Text.RegularExpressions.Regex.Replace(Content, @"\s+", " ").Trim();

			// Prend les 60 premiers caractères
			return cleaned[..Math.Min(60, cleaned.Length)];
		}
	}

	// Se remplissent à partir de DateSent
	public DateOnly? DaySent => DateSent is null ? null : DateOnly.FromDateTime(DateSent.Value);

	public TimeOnly? TimeSent => DateSent is null ? null : TimeOnly.FromDateTime(DateSent.Value);

	public bool IsSameDay => DateSent.HasValue && DateSent.Value.Date == DateTime.Today;

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
}
