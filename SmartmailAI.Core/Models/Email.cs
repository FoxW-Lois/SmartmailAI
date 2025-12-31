using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace SmartmailAI.Core.Models;

public class Email
{
	public string SenderName { get; set; }
	public string SenderEmail { get; set; }
	public Uri? SenderProfileImage { get; set; }

	public ImageSource SenderProfileImageSource
	{
		get
		{
			if (SenderProfileImage == null)
				return new BitmapImage(new Uri("ms-appx:///Assets/Content/Default-Avatar-icon.jpg"));

			return new BitmapImage(SenderProfileImage);
		}
	}

	public string? ReceiverName { get; set; }
	public string? ReceiverEmail { get; set; }
	public Uri? ReceiverProfileImage { get; set; }

	public ImageSource ReceiverProfileImageSource
	{
		get
		{
			if (ReceiverProfileImage == null)
				return new BitmapImage(new Uri("ms-appx:///Assets/Content/Default-Avatar-icon.jpg"));

			return new BitmapImage(ReceiverProfileImage);
		}
	}

	public string? Subject { get; set; }
	public string? Content { get; set; }

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

	public DateTime? DateSent { get; set; }

	public DateOnly? DaySent => DateSent is null ? null : DateOnly.FromDateTime(DateSent.Value);

	public TimeOnly? TimeSent => DateSent is null ? null : TimeOnly.FromDateTime(DateSent.Value);

	public List<string>? Attachments { get; set; }

	public string AttachmentsDisplay => Attachments != null && Attachments.Count != 0
		? string.Join(", ", Attachments) : "Aucune pièce jointe";

	// Pour la catégorisation
	public MailboxType MailboxType { get; set; }
}
