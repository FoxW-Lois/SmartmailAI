using System.Collections.Generic;
using System.Collections.ObjectModel;
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
		}
	}

	#region Propriétés pour le refresh graphique

	private ObservableCollection<Email> _items = [];
	public ObservableCollection<Email> ItemsCollection => _items;
	public MailboxType MailboxType { get; set; }

	#endregion Propriétés pour le refresh graphique

	// Pour le second ListDetailsView
	public Email SelectedEmail { get; set; }
}
