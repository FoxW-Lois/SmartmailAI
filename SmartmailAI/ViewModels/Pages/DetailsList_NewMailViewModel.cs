using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace SmartmailAI.ViewModels.Pages;

public partial class DetailsList_NewMailViewModel : ObservableObject
{
	[ObservableProperty]
	private string _to = string.Empty;

	[ObservableProperty]
	private string _cc = string.Empty;

	[ObservableProperty]
	private string _bcc = string.Empty;

	[ObservableProperty]
	private string _subject = string.Empty;

	[ObservableProperty]
	private string _body = string.Empty;

	[ObservableProperty]
	private bool _isBccVisible;

	[RelayCommand]
	private async Task SendAsync()
	{
		// TODO : brancher l'envoi SMTP ici
	}

	[RelayCommand]
	private void Discard()
	{
		// Notifie DetailsList_ViewModel de fermer le ComposeOverlay
		WeakReferenceMessenger.Default.Send(new CloseComposeMessage());
		Reset();
	}

	[RelayCommand]
	private void Expand()
	{
		// TODO : ouvrir en plein écran
	}

	[RelayCommand]
	private void ToggleBcc()
	{
		IsBccVisible = !IsBccVisible;
	}

	private void Reset()
	{
		To = string.Empty;
		Cc = string.Empty;
		Bcc = string.Empty;
		Subject = string.Empty;
		Body = string.Empty;
		IsBccVisible = false;
	}
}
