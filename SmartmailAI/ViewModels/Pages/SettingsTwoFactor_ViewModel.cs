using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.ApplicationModel.Resources;
using SmartmailAI.Core.Contracts.Services.LocalSecurity;
using SmartmailAI.Core.Data;

namespace SmartmailAI.ViewModels.Pages;

public partial class SettingsTwoFactor_ViewModel : ObservableRecipient, INavigationAware
{
	private readonly ITotpService _totpService;
	private readonly IQrCodeService _qrCodeService;
	private readonly IDpapiService _dpapiService;
	private readonly IAccountSecretStore _secretStore;
	private readonly IAuthService _authService;
	private readonly INavigationService _navigationService;

	private string _errorMessage = string.Empty;
	private readonly ResourceLoader resourceLoader = new();
	private TotpSecret? _tempSecret;

	#region ObservableProperties & View Properties

	public Visibility ErrorVisibility => string.IsNullOrWhiteSpace(ErrorMessage) ? Visibility.Collapsed : Visibility.Visible;

	public BitmapImage? QrCodeImage { get; private set; }

	[ObservableProperty]
	public partial string Code { get; set; } = string.Empty;

	[ObservableProperty]
	public partial bool IsQrVisible { get; set; } = false;

	#endregion ObservableProperties & View Properties

	public string ErrorMessage
	{
		get => _errorMessage;
		set
		{
			SetProperty(ref _errorMessage, value);
			OnPropertyChanged(nameof(ErrorVisibility));
		}
	}

	public ICommand Confirm2FACommand { get; }

	public SettingsTwoFactor_ViewModel(ITotpService totpService, IQrCodeService qrCodeService, IDpapiService dpapiService,
		IAccountSecretStore secretStore, IAuthService authService, INavigationService navigationService)
	{
		_totpService = totpService;
		_qrCodeService = qrCodeService;
		_dpapiService = dpapiService;
		_secretStore = secretStore;
		_authService = authService;
		_navigationService = navigationService;

		Confirm2FACommand = new RelayCommand(Confirm2FA);
	}

	private void StartEnable2FA()
	{
		// Génération de la clé temporaire
		_tempSecret = _totpService.GenerateSecret();

		// Génération de l’URI OTP
		var uri = _totpService.GenerateOtpAuthUri("SmartmailAI", _authService.CurrentAccountLogin, _tempSecret.Base32);

		// Génération du QR Code
		var qrBytes = _qrCodeService.GenerateQrCode(uri);
		QrCodeImage = _qrCodeService.CreateBitmapImage(qrBytes);
		OnPropertyChanged(nameof(QrCodeImage));

		IsQrVisible = true;
	}

	private async void Confirm2FA()
	{
		if (_tempSecret is null)
			return;

		// Validation du code saisi
		var isValid = _totpService.ValidateCode(_tempSecret.Base32, Code);

		if (!isValid)
		{
			ErrorMessage = resourceLoader.GetString("Error_SecondFactorInvalid");
			return;
		}

		ErrorMessage = string.Empty;

		// Chiffrement et stockage définitif
		var encryptedSecret = _dpapiService.Encrypt(_tempSecret.Base32);

		await _secretStore.SaveSecretAsync(_authService.CurrentAccountLogin, encryptedSecret);

		// Nettoyage des champs
		_tempSecret = null;
		Code = string.Empty;
		IsQrVisible = false;

		await Task.Delay(3000); // Attente de 3 secondes
		_navigationService.NavigateTo(typeof(Settings_ViewModel).FullName!);
	}

	public async Task OnNavigatedTo(object? parameter)
	{
		StartEnable2FA();
	}

	public void OnNavigatedFrom()
	{
	}
}
