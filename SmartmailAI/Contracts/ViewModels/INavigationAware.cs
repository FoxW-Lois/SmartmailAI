namespace SmartmailAI.Contracts.ViewModels;

internal interface INavigationAware
{
	Task OnNavigatedTo(object parameter);

	void OnNavigatedFrom();
}
