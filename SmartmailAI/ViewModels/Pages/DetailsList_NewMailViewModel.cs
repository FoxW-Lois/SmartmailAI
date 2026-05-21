using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Windows.ApplicationModel.Resources;
using SmartmailAI.Core.Contracts.Services.Addresses;
using SmartmailAI.Core.Models.Messengers;
using SmartmailAI.Core.Services.Addresses;
using Windows.Storage.Pickers;

namespace SmartmailAI.ViewModels.Pages;

public partial class DetailsList_NewMailViewModel : ObservableObject
{
	private readonly IAddressesService _addressesService;
	private readonly IGmailApiService _gmailApiService;
	private readonly IGmailCredentialService _gmailCredentialService;
	private readonly IOtherProtocolService _otherProtocolService;
	private readonly IOtherCredentialService _otherCredentialService;
	private readonly IOtherTokenStore _otherTokenStore;
	private readonly IDialogService _dialogService;
	private readonly ResourceLoader resourceLoader = new();

	public ObservableCollection<MailAttachment> Attachments { get; } = [];
	public bool HasAttachments => Attachments.Count > 0;

	public DetailsList_NewMailViewModel(IAddressesService addressesService, IGmailApiService gmailApiService, IGmailCredentialService gmailCredentialService,
		IOtherProtocolService otherProtocolService, IOtherCredentialService otherCredentialService, IOtherTokenStore otherTokenStore,
		IDialogService dialogService)
	{
		_addressesService = addressesService;
		_gmailApiService = gmailApiService;
		_gmailCredentialService = gmailCredentialService;
		_otherProtocolService = otherProtocolService;
		_otherCredentialService = otherCredentialService;
		_otherTokenStore = otherTokenStore;
		_dialogService = dialogService;

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

		var accountMail = await _addressesService.GetAccountByEmailAsync(_from);

		if (accountMail is null)
		{
			await _dialogService.ShowOneButtonDialogAsync(resourceLoader.GetString("Error_Title"),
				resourceLoader.GetString("Error_AccountUnfound_Gmail"));
			return;
		}

		if (accountMail is AccountGmail accountGmail)
		{
			var credential = await _gmailCredentialService.GetCredentialAsync(accountGmail!);

			if (credential is null)
			{
				await _dialogService.ShowOneButtonDialogAsync(resourceLoader.GetString("Error_Title"),
					resourceLoader.GetString("Error_AccountUnfound_Gmail"));
				return;
			}

			try
			{
				await _gmailApiService.SendEmailAsync(credential, To, Subject, Body, Attachments);
			}
			catch (Exception)
			{
				await _dialogService.ShowOneButtonDialogAsync(resourceLoader.GetString("Error_Title"),
					resourceLoader.GetString("Error_EmailSendingFailed"));
				return;
			}
		}
		else if (accountMail is AccountOther accountOther)
		{
			string? password = await _otherTokenStore.GetPasswordAsync(accountOther.TokenStorageKey);
			if (password == null) return;

			// On récupère le mot de passe stocké pour le compte et on le set dans l'accountOther pour pouvoir se connecter via IMAP/SMTP
			accountOther.Password = password;

			var success = await _otherCredentialService.ConnectAsync(accountOther);

			if (!success)
			{
				await _dialogService.ShowOneButtonDialogAsync(resourceLoader.GetString("Error_Title"),
					resourceLoader.GetString("Error_AccountUnfound_Other"));
				return;
			}

			try
			{
				await _otherProtocolService.SendEmailAsync(accountOther, To, Subject, Body, Attachments);
			}
			catch (Exception)
			{
				await _dialogService.ShowOneButtonDialogAsync(resourceLoader.GetString("Error_Title"),
					resourceLoader.GetString("Error_EmailSendingFailed"));
				return;
			}
		}

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
