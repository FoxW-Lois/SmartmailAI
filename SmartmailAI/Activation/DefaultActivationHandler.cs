using Microsoft.UI.Xaml;

namespace SmartmailAI.Activation;

internal class DefaultActivationHandler(INavigationService navigationService) : ActivationHandler<LaunchActivatedEventArgs>
{
	private readonly INavigationService _navigationService = navigationService;

	protected override bool CanHandleInternal(LaunchActivatedEventArgs args)
	{
		// None of the ActivationHandlers has handled the activation.
		return _navigationService.Frame?.Content is null;
	}

	protected override async Task HandleInternalAsync(LaunchActivatedEventArgs args)
	{
		// Initialize to home page.
		_navigationService.NavigateTo(typeof(Home_ViewModel).FullName!, args.Arguments);

		await Task.CompletedTask;
	}
}
