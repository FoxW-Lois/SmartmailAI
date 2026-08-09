using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Windows.ApplicationModel.Resources;
using SmartmailAI.Core.Contracts.Repository;
using SmartmailAI.Core.Models.Messengers;

namespace SmartmailAI.ViewModels.Pages;

public partial class UXQuestions_ViewModel(IAccountRepository accountRepository, IAccountService accountService) : ObservableObject
{
	private readonly IAccountRepository _accountRepository = accountRepository;
	private readonly IAccountService _accountService = accountService;
	private readonly ResourceLoader resourceLoader = new();
	private DateOnly? parsedDatePicked;

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

	#region ErrorMessage Properties

	[ObservableProperty]
	public partial string? ErrorMessage1 { get; set; }

	[ObservableProperty]
	public partial string? ErrorMessage2 { get; set; }

	[ObservableProperty]
	public partial string? ErrorMessage3 { get; set; }

	public bool HasError1 => string.IsNullOrWhiteSpace(ErrorMessage1);
	public bool HasError2 => string.IsNullOrWhiteSpace(ErrorMessage2);
	public bool HasError3 => string.IsNullOrWhiteSpace(ErrorMessage3);

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

		// Notifie Home_ViewModel, NavShell_ViewModel et Settings_ViewModel de mettre à jour leur vue respective
		WeakReferenceMessenger.Default.Send(new RequestUpdateUXQuestionsMessage { ChangeDisplay = true });

		Reset();
	}

	private string GetErrorMessage(bool condition, string resourceKey)
	{
		return condition ? resourceLoader.GetString(resourceKey) : string.Empty;
	}

	partial void OnNbOpenAppByWeekChanged(int value)
	{
		if (double.IsNaN(value) || value < 0)
		{
			NbOpenAppByWeek = 0;
		}
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
