using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.Resources;
using SmartmailAI.Core.Contracts.Repository;

namespace SmartmailAI.ViewModels.Pages;

public partial class AddressManagement_ViewModel : ObservableRecipient
{
	private readonly IAddressesRepository _addressRepository;
	private readonly IAddressesService _addressesService;
	private readonly INavigationService _navigationService;
	private string _errorMessage = string.Empty;
	private readonly ResourceLoader resourceLoader = new();

	[ObservableProperty]
	private ObservableCollection<AccountMailBase> accountsMail = [];

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

	public AddressManagement_ViewModel(IAddressesRepository addressRepository, IAddressesService addressesService, INavigationService navigationService)
	{
		_addressRepository = addressRepository;
		_addressesService = addressesService;
		_navigationService = navigationService;
	}

	public async Task LoadAddressesAsync()
	{
		var result = await _addressRepository.GetAllAddressesAsync();
		AccountsMail = new ObservableCollection<AccountMailBase>(result);
	}

	public async Task DeleteAddressAsync(AccountGmail? accountGmail = null, AccountOther? accountOther = null)
	{
		ErrorMessage = string.Empty;

		bool success = false;

		if (accountGmail != null)
			success = await _addressesService.RemoveGmailAccountAsync(accountGmail);
		else if (accountOther != null)
			success = await _addressesService.RemoveOtherAccountAsync(accountOther);

		if (!success)
		{
			ErrorMessage = resourceLoader.GetString("Error_DeleteAddress");
			return;
		}

		if (accountGmail != null)
			AccountsMail.Remove(accountGmail);
		else if (accountOther != null)
			AccountsMail.Remove(accountOther);

		await _addressesService.RefreshAddressesListAsync();

		// Le 3ème paramètre (true) nettoie l'historique de navigation
		_navigationService.NavigateTo(typeof(AddAddress_ViewModel).FullName!, null, true);
		return;
	}
}
