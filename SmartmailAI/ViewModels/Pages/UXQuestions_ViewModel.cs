using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.Resources;
using SmartmailAI.Core.Contracts.Repository;
using SmartmailAI.Core.Models.Messengers;

namespace SmartmailAI.ViewModels.Pages;

public partial class UXQuestions_ViewModel(IAccountRepository accountRepository, IAccountService accountService) : ObservableObject
{
	private readonly IAccountRepository _accountRepository = accountRepository;
	private readonly IAccountService _accountService = accountService;

	#region ObservableProperties

	[ObservableProperty]
	public partial int NbOpenAppByWeek { get; set; } = 0;

	[ObservableProperty]
	public partial ObservableCollection<string> AverageDailyTraficOptions { get; set; } = ["1 à 30 mails par jour",
		"30 à 60 mails par jour", "60 à 90 mails par jour", "+ de 90 mails par jour"];

	[ObservableProperty]
	public partial string SelectedAverageDailyTrafic { get; set; }

	[ObservableProperty]
	public partial bool RetrievedAllEmails { get; set; }

	[ObservableProperty]
	public partial DateTimeOffset? DatePicked { get; set; }

	#endregion ObservableProperties

	private DateOnly? parsedDatePicked;
	private string _errorMessage1 = string.Empty;
	private string _errorMessage2 = string.Empty;
	private string _errorMessage3 = string.Empty;
	private readonly ResourceLoader resourceLoader = new();

	#region ErrorMessage Properties

	public string ErrorMessage1
	{
		get => _errorMessage1;
		set
		{
			SetProperty(ref _errorMessage1, value);
			OnPropertyChanged(nameof(ErrorVisibility1));
		}
	}

	public string ErrorMessage2
	{
		get => _errorMessage2;
		set
		{
			SetProperty(ref _errorMessage2, value);
			OnPropertyChanged(nameof(ErrorVisibility2));
		}
	}

	public string ErrorMessage3
	{
		get => _errorMessage3;
		set
		{
			SetProperty(ref _errorMessage3, value);
			OnPropertyChanged(nameof(ErrorVisibility3));
		}
	}

	public Visibility ErrorVisibility1 => string.IsNullOrWhiteSpace(ErrorMessage1) ? Visibility.Collapsed : Visibility.Visible;
	public Visibility ErrorVisibility2 => string.IsNullOrWhiteSpace(ErrorMessage2) ? Visibility.Collapsed : Visibility.Visible;
	public Visibility ErrorVisibility3 => string.IsNullOrWhiteSpace(ErrorMessage3) ? Visibility.Collapsed : Visibility.Visible;

	#endregion ErrorMessage Properties

	[RelayCommand]
	public async Task ValidateFormAsync()
	{
		ErrorMessage1 = GetErrorMessage(NbOpenAppByWeek < 1 || NbOpenAppByWeek > 100, "Error_FormInvalid_NbOpenAppByWeek");
		ErrorMessage2 = GetErrorMessage(string.IsNullOrWhiteSpace(SelectedAverageDailyTrafic), "Error_FormInvalid_SelectedAverageDailyTrafic");
		ErrorMessage3 = GetErrorMessage(!RetrievedAllEmails && !DatePicked.HasValue, "Error_FormInvalid_DatePicked");

		if (!string.IsNullOrWhiteSpace(ErrorMessage1) || !string.IsNullOrWhiteSpace(ErrorMessage2) || !string.IsNullOrWhiteSpace(ErrorMessage3))
			return;

		var account = await _accountService.GetAccountByLoginInLocalSessionAsync();

		if (account is null)
			return;

		account.IsFirstConnection = false;
		account.NbOpenAppByWeek = NbOpenAppByWeek;
		account.AverageDailyTrafic = SelectedAverageDailyTrafic;
		account.RetrievedAllEmails = RetrievedAllEmails;
		account.DatePicked = parsedDatePicked;

		await _accountRepository.UpdateAccountAsync(account);

		// Notifie Home_ViewModel et NavShell_ViewModel de mettre à jour leur vue respective
		WeakReferenceMessenger.Default.Send(new RequestUpdateUXQuestionsMessage());

		Reset();
	}

	private string GetErrorMessage(bool condition, string resourceKey)
	{
		return condition ? resourceLoader.GetString(resourceKey) : string.Empty;
	}

	partial void OnDatePickedChanged(DateTimeOffset? value)
	{
		if (value is null)
			return;

		parsedDatePicked = DateOnly.FromDateTime(DateTime.Parse(value.Value.ToString("yyyy-MM-dd")));
	}

	private void Reset()
	{
		NbOpenAppByWeek = 0;
		SelectedAverageDailyTrafic = string.Empty;
		RetrievedAllEmails = false;
		DatePicked = null;
	}
}
