using Microsoft.Windows.ApplicationModel.Resources;
using SmartmailAI.Core.Contracts.Repository;
using SmartmailAI.Core.Contracts.Services.Addresses;

namespace SmartmailAI.Services;

public class EmailLoaderService(IAuthService authService, IMailReaderService mailReaderService, IEmailRepository emailRepository,
	IDialogService dialogService) : IEmailLoaderService
{
	private readonly IAuthService _authService = authService;
	private readonly IMailReaderService _mailReaderService = mailReaderService;
	private readonly IEmailRepository _emailRepository = emailRepository;

	private readonly IDialogService _dialogService = dialogService;
	private readonly ResourceLoader resourceLoader = new();

	public async Task LoadMessagesAsync(bool isAddingNewAddress, AccountMailBase account)
	{
		IReadOnlyList<Email>? mails = null;

		while (mails is null)
		{
			mails = await _mailReaderService.GetLastMessagesFromAccountAsync(isAddingNewAddress, account);

			if (mails is not null) break;

			await _dialogService.ShowOneButtonDialogAsync(resourceLoader.GetString("Error_Title"),
				resourceLoader.GetString("Error_HasNoInternet"));
		}

		foreach (var email in mails)
			await _emailRepository.AddEmailAsync(email);

		await _authService.UpdateLastConnection();
	}
}
