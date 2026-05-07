namespace SmartmailAI.Core.Contracts.Services.Addresses;

public interface IOutlookTokenStore
{
	void SaveAccountId(string tokenStorageKey, string homeAccountId);

	string? GetAccountId(string tokenStorageKey);

	void DeleteToken(string tokenStorageKey);
}
