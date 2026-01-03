using System.Collections.Generic;

namespace SmartmailAI.Core.Models;

public class MailboxCategory
{
	public string Title { get; set; }
	public string Icon { get; set; }
	public IEnumerable<Email> Items { get; set; }


	// Pour le second ListDetailsView
	public Email SelectedEmail { get; set; }
}
