using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SmartmailAI.Core.Models;

public class MailboxCategory : ObservableObject
{
	public string Title { get; set; }
	public string Icon { get; set; }

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
	public Email SelectedEmail { get; set; }

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
		ItemsCollection.Clear();

		if (string.IsNullOrWhiteSpace(filter))
		{
			foreach (var mail in _allItems)
				ItemsCollection.Add(mail);
			return;
		}

		var lower = filter.ToLowerInvariant();

		foreach (var mail in _allItems.Where(mail =>
			(mail.SenderName?.ToLowerInvariant().Contains(lower) ?? false) ||
			(mail.Subject?.ToLowerInvariant().Contains(lower) ?? false) ||
			(mail.PreviewContent?.ToLowerInvariant().Contains(lower) ?? false)))
		{
			ItemsCollection.Add(mail);
		}
	}
}
