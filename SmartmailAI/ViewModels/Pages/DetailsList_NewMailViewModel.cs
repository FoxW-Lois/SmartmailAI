using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SmartmailAI.Core.Contracts.Services.Addresses;
using SmartmailAI.Core.Models.Composers;
using Windows.Storage.Pickers;

namespace SmartmailAI.ViewModels.Pages;

public partial class DetailsList_NewMailViewModel : ObservableObject
{
	private readonly IAddressesService _addressesService;
	private readonly IGmailApiService _gmailApiService;
	private readonly IGmailCredentialService _gmailCredentialService;

	public ObservableCollection<MailAttachment> Attachments { get; } = [];
	public bool HasAttachments => Attachments.Count > 0;

	public DetailsList_NewMailViewModel(IAddressesService addressesService, IGmailApiService gmailApiService, IGmailCredentialService gmailCredentialService)
	{
		_addressesService = addressesService;
		_gmailApiService = gmailApiService;
		_gmailCredentialService = gmailCredentialService;

		WeakReferenceMessenger.Default.Register<OpenComposeMessage>(this, (r, m) =>
		{
			_from = m.SenderEmail;
		});

		Attachments.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HasAttachments));
	}

	private string _from = string.Empty;

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
		if (string.IsNullOrWhiteSpace(To) || string.IsNullOrWhiteSpace(Subject))
			return;

		var accountGmail = await _addressesService.GetAccountByEmailAsync(_from);
		var credential = await _gmailCredentialService.GetCredentialAsync(accountGmail!);

		// TODO : Gérer le cas où le compte Gmail/credential sont invalides (afficher un message d'erreur)
		if (credential is null)
			return;

		await _gmailApiService.SendEmailAsync(credential, To, Subject, Body, Attachments);

		// Notifie DetailsList_ViewModel de fermer le ComposeOverlay
		Discard();
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

	#region Commandes de rédaction d'email

	[RelayCommand]
	private async Task AttachFileAsync()
	{
		var picker = new FileOpenPicker();
		picker.FileTypeFilter.Add("*");

		// Nécessaire en WinUI3 pour associer le picker à la fenêtre
		var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
		WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

		var files = await picker.PickMultipleFilesAsync();
		foreach (var file in files)
			AddAttachment(file.Path, file.Name);
	}

	[RelayCommand]
	private void RemoveAttachment(MailAttachment attachment)
	{
		Attachments.Remove(attachment);
	}

	public void AddAttachment(string path, string name)
	{
		if (Attachments.Any(a => a.FilePath == path))
			return;

		Attachments.Add(new MailAttachment
		{
			FileName = name,
			FilePath = path
		});
	}

	#endregion Commandes de rédaction d'email
}
