using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Windows.ApplicationModel.Resources;
using SmartmailAI.Core.Contracts.Repository;

namespace SmartmailAI.ViewModels.Pages;

public partial class AddressManagement_ViewModel(IAddressesRepository addressRepository, IAddressesService addressesService,
	INavigationService navigationService) : ObservableRecipient
{
	private readonly IAddressesRepository _addressRepository = addressRepository;
	private readonly IAddressesService _addressesService = addressesService;
	private readonly INavigationService _navigationService = navigationService;
	private readonly ResourceLoader resourceLoader = new();

	#region ObservableProperties & View Properties

	[ObservableProperty]
	public partial ObservableCollection<AccountMailBase> AccountsMail { get; set; } = [];

	[ObservableProperty]
	public partial string? ErrorMessage { get; set; }

	public bool HasError => string.IsNullOrWhiteSpace(ErrorMessage);

	#endregion ObservableProperties & View Properties

	public async Task LoadAddressesAsync()
	{
		var result = await _addressRepository.GetAllAddressesByAccountIndexGuidAsync();
		AccountsMail = new ObservableCollection<AccountMailBase>(result);
	}

	public async Task DeleteAddressAsync(AccountMailBase account)
	{
		ErrorMessage = null;

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
