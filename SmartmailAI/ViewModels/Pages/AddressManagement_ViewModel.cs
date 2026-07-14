using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.Resources;
using SmartmailAI.Core.Contracts.Repository;

namespace SmartmailAI.ViewModels.Pages;

public partial class AddressManagement_ViewModel(IAddressesRepository addressRepository, IAddressesService addressesService,
	INavigationService navigationService) : ObservableRecipient
{
	private readonly IAddressesRepository _addressRepository = addressRepository;
	private readonly IAddressesService _addressesService = addressesService;
	private readonly INavigationService _navigationService = navigationService;
	private string _errorMessage = string.Empty;
	private readonly ResourceLoader resourceLoader = new();

	[ObservableProperty]
	public partial ObservableCollection<AccountMailBase> AccountsMail { get; set; } = [];

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

	public async Task LoadAddressesAsync()
	{
		var result = await _addressRepository.GetAllAddressesAsync();
		AccountsMail = new ObservableCollection<AccountMailBase>(result);
	}

	public async Task DeleteAddressAsync(AccountMailBase account)
	{
		ErrorMessage = string.Empty;

		bool success = await _addressesService.RemoveAddressAsync(account);

		if (!success)
		{
			ErrorMessage = resourceLoader.GetString("Error_DeleteAddress");
			return;
		}

		AccountsMail.Remove(account);

		await _addressesService.RefreshAddressesListAsync();

		// Le 3ème paramètre (true) nettoie l'historique de navigation
		_navigationService.NavigateTo(typeof(AddAddress_ViewModel).FullName!, null, true);
		return;
	}
}
