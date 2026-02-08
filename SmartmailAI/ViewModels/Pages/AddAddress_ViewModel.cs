using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.Resources;
using SmartmailAI.Core.Contracts.Services.Addresses;
using SmartmailAI.Core.IRepository;

namespace SmartmailAI.ViewModels.Pages;

public partial class AddAddress_ViewModel(IAddressesService addressesService, IMailReaderService railReaderService,
	IEmailRepository emailRepository, IAuthService authService) : ObservableRecipient
{
	[ObservableProperty]
	public partial bool IsOtherChoice { get; set; }

	private readonly IAddressesService _addressesService = addressesService;
	private readonly IMailReaderService _mailReaderService = railReaderService;
	private readonly IEmailRepository _emailRepository = emailRepository;
	private readonly IAuthService _authService = authService;
	private string _email = string.Empty;
	private string _errorMessage = string.Empty;
	private readonly ResourceLoader resourceLoader = new();

	public string Email
	{
		get => _email;
		set => SetProperty(ref _email, value);
	}

	public string ErrorMessage
	{
		get => _errorMessage;
		set
		{
			SetProperty(ref _errorMessage, value);
			OnPropertyChanged(nameof(ErrorVisibility));
		}
	}

	public Visibility ErrorVisibility => string.IsNullOrWhiteSpace(ErrorMessage) ? Visibility.Collapsed : Visibility.Visible;

	public ObservableCollection<EmailGmail> Messages { get; } = [];

	public async Task LoadMessagesAsync()
	{
		Messages.Clear();

		var mails = await _mailReaderService.GetLastMessagesFromFirstAccountAsync();

		foreach (var email in mails)
			await _emailRepository.AddAsync(email);

		await _authService.UpdateLastConnection();
	}

	#region Add methods for diffrent Addresses type/domain/source

	public async Task<bool> AddOAuth2Async()
	{
		ErrorMessage = string.Empty;

		(_, bool success) = await _addressesService.AddGmailAccountAsync();

		if (!success)
		{
			ErrorMessage = resourceLoader.GetString("Error_RecoveryEmailOAuth2Invalid");
			return false;
		}

		await _addressesService.RefreshAddressesListAsync();
		await LoadMessagesAsync();
		return true;
	}

	public async Task<bool> OnAddOutlookAsync()
	{
		ErrorMessage = string.Empty;

		bool success = await _addressesService.AddOutlookAsync();

		if (!success)
		{
			ErrorMessage = resourceLoader.GetString("Error_RecoveryEmailOutlookInvalid");
			return false;
		}

		await _addressesService.RefreshAddressesListAsync();
		return true;
	}

	public async Task<bool> AddOtherAddressAsync(string password)
	{
		ErrorMessage = string.Empty;

		bool success = await _addressesService.AddOtherAddressAsync();

		if (!success)
		{
			ErrorMessage = resourceLoader.GetString("Error_EmailOrPasswordInvalid");
			return false;
		}

		await _addressesService.RefreshAddressesListAsync();
		return true;
	}

	#endregion Add methods for diffrent Addresses type/domain/source
}
