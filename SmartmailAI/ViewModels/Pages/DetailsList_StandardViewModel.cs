using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SmartmailAI.Core.Contracts.Services.Addresses;
using SmartmailAI.Core.Models.Messengers;

namespace SmartmailAI.ViewModels.Pages;

public partial class DetailsList_StandardViewModel(IMailReaderService mailReaderService, IAddressesService addressesService) : ObservableRecipient
{
	private readonly IMailReaderService _mailReaderService = mailReaderService;
	private readonly IAddressesService _addressesService = addressesService;

	[RelayCommand]
	private async Task SaveAttachmentAsync((string emailGuid, MailAttachment attachment, string destinationFolder) args)
	{
		// Désabonne d'abord si déjà enregistré
		WeakReferenceMessenger.Default.Unregister<ResponseAddressAccountMessage>(this);

		string resolvedAddress = string.Empty;

		// Demande l'adresse uniquement si l'utilisateur récupère une pièce jointe
		WeakReferenceMessenger.Default.Register<ResponseAddressAccountMessage>(this, (r, m) =>
		{
			resolvedAddress = m.AddressAccount;
			WeakReferenceMessenger.Default.Unregister<ResponseAddressAccountMessage>(this);
		});

		WeakReferenceMessenger.Default.Send(new RequestAddressAccountMessage());

		var account = await _addressesService.GetAccountByEmailAsync(resolvedAddress);

		// TODO : afficher une erreur à l'utilisateur si échec
		if (account is null)
			return;

		await _mailReaderService.SaveAttachmentFromEmailAsync(account, args.emailGuid, args.attachment, args.destinationFolder);

		// TODO : afficher une erreur à l'utilisateur si échec
	}
}
