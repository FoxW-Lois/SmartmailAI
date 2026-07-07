using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SmartmailAI.Core.Models;

public partial class MailboxCategory : ObservableObject
{
	public string Title { get; set; } = null!;
	public string Icon { get; set; } = null!;

	public IEnumerable<Email> Items
	{
		get => _items;
		set
		{
			if (value is ObservableCollection<Email> oc)
				SetProperty(ref _items, oc);
			else
				SetProperty(ref _items, new ObservableCollection<Email>(value));

			// Update de _allItems => prend à l'initialisation une copie de tous les mails
			_allItems = new ObservableCollection<Email>(_items);
		}
	}

	#region Propriétés pour le refresh graphique

	private ObservableCollection<Email> _items = [];

	// Collection exposée pour le binding
	public ObservableCollection<Email> ItemsCollection => _items;

	// Collection interne pour garder tous les mails et les filtrer par la suite
	private ObservableCollection<Email> _allItems = [];

	public MailboxType MailboxType { get; set; }

	#endregion Propriétés pour le refresh graphique

	// Pour le second ListDetailsView
	public Email SelectedEmail { get; set; } = null!;

	// Update des mails triés par le filtre (_allItems)
	public void ReplaceAllItems(IEnumerable<Email> emails)
	{
		_allItems.Clear();
		ItemsCollection.Clear();

		foreach (var email in emails)
		{
			_allItems.Add(email);
			ItemsCollection.Add(email);
		}
	}

	// Méthode de filtrage des mails
	public void ApplyFilter(string filter)
	{
		var filteredMails = _allItems.AsEnumerable();

		if (string.IsNullOrWhiteSpace(filter))
			return;

		// Regex pour des filtres spéciaux
		var dateBeforeMatch = Regex.Match(filter, @"Date:Before:(\d{4}-\d{2}-\d{2})", RegexOptions.IgnoreCase);
		var dateAfterMatch = Regex.Match(filter, @"Date:After:(\d{4}-\d{2}-\d{2})", RegexOptions.IgnoreCase);
		var attachmentYesMatch = Regex.Match(filter, @"Attachment:Yes", RegexOptions.IgnoreCase);
		var attachmentNoMatch = Regex.Match(filter, @"Attachment:No", RegexOptions.IgnoreCase);

		if (dateBeforeMatch.Success && DateTime.TryParseExact(dateBeforeMatch.Groups[1].Value, "yyyy-MM-dd",
			CultureInfo.InvariantCulture, DateTimeStyles.None, out var searchedDateBefore))
		{
			filteredMails = filteredMails.Where(m => m.DateSent.HasValue && m.DateSent.Value.Date <= searchedDateBefore.Date);
			filter = filter.Replace(dateBeforeMatch.Value, "");
		}

		if (dateAfterMatch.Success && DateTime.TryParseExact(dateAfterMatch.Groups[1].Value, "yyyy-MM-dd",
			CultureInfo.InvariantCulture, DateTimeStyles.None, out var searchedDateAfter))
		{
			filteredMails = filteredMails.Where(m => m.DateSent.HasValue && m.DateSent.Value.Date >= searchedDateAfter.Date);
			filter = filter.Replace(dateAfterMatch.Value, "");
		}

		if (attachmentYesMatch.Success)
		{
			filteredMails = filteredMails.Where(m => m.Attachments is not null && m.Attachments.Count > 0);
			filter = filter.Replace(attachmentYesMatch.Value, "");
		}

		if (attachmentNoMatch.Success)
		{
			filteredMails = filteredMails.Where(m => m.Attachments is not null && m.Attachments.Count == 0);
			filter = filter.Replace(attachmentNoMatch.Value, "");
		}

		var searchText = filter.Trim();

		if (!string.IsNullOrWhiteSpace(searchText))
		{
			var lower = searchText.ToLowerInvariant();

			filteredMails = filteredMails.Where(mail =>
				(mail.SenderName?.Contains(lower, StringComparison.OrdinalIgnoreCase) ?? false) ||
				(mail.Subject?.Contains(lower, StringComparison.OrdinalIgnoreCase) ?? false) ||
				(mail.Content?.Contains(lower, StringComparison.OrdinalIgnoreCase) ?? false)
			);
		}

		ItemsCollection.Clear();
		foreach (var mail in filteredMails)
			ItemsCollection.Add(mail);
	}
}
