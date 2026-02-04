using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.Resources;
using SmartmailAI.Core.IRepository;

namespace SmartmailAI.ViewModels.Pages;

public partial class AddressManagement_ViewModel : ObservableRecipient
{
	private readonly IAddressesRepository _addressRepository;
	private readonly IAddressesService _addressesService;
	private string _errorMessage = string.Empty;
	private readonly ResourceLoader resourceLoader = new();

	[ObservableProperty]
	private ObservableCollection<AccountGmail> accountsGmail = [];

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

	public AddressManagement_ViewModel(IAddressesRepository addressRepository, IAddressesService addressesService)
	{
		_addressRepository = addressRepository;
		_addressesService = addressesService;
	}

	public async Task LoadAddressesAsync()
	{
		var result = await _addressRepository.GetAllAddressAsync();
		AccountsGmail = new ObservableCollection<AccountGmail>(result);
	}

	public async Task DeleteAddressAsync(AccountGmail accountGmail)
	{
		ErrorMessage = string.Empty;

		bool success = await _addressesService.RemoveGmailAccountAsync(accountGmail);
		
		if (!success)
		{
			ErrorMessage = resourceLoader.GetString("Error_DeleteAddress");
			return;
		}

		AccountsGmail.Remove(accountGmail);
		return;
	}
}
