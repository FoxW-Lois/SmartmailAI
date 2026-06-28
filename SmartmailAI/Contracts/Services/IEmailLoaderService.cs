namespace SmartmailAI.Contracts.Services;

public interface IEmailLoaderService
{
	Task LoadMessagesAsync(bool isAddingNewAddress, AccountMailBase account);
}
