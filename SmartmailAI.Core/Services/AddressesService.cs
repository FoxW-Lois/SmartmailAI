using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using SmartmailAI.Core.Contracts.Services;
using SmartmailAI.Core.Contracts.Services.Addresses;
using SmartmailAI.Core.IRepository;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Services;

public class AddressesService(IAddressesRepository addressRepository, IOAuthGmailService oAuthGmailService, IGmailApiService gmailApiService,
	IGmailLogoutService gmailLogoutService) : IAddressesService
{
	private readonly IAddressesRepository _addressRepository = addressRepository;
	private readonly IOAuthGmailService _oAuthGmailService = oAuthGmailService;
	private readonly IGmailApiService _gmailApiService = gmailApiService;
	private readonly IGmailLogoutService _gmailLogoutService = gmailLogoutService;

	public bool HasAny { get; private set; }

	public event EventHandler<bool>? AddressesListChanged;
	public async Task RefreshAddressesListAsync()
	{
		var newValue = await _addressRepository.GetAllAddressAsync();
		HasAny = newValue.Count > 0;
		AddressesListChanged?.Invoke(this, HasAny);
	}

	public async Task<(AccountGmail, bool)> AddGmailAccountAsync()
	{
		var userKey = Guid.NewGuid().ToString();

		var credential = await _oAuthGmailService.ConnectAsync(userKey);
		var email = await _gmailApiService.GetEmailAddressAsync(credential);

		var account = new AccountGmail
		{
			Email = email,
			GoogleUserId = credential.UserId,
			ConnectedAt = DateTime.UtcNow,
			TokenStorageKey = userKey
		};

		await _addressRepository.AddAsync(account);
		return (account, true);
	}

	public async Task<bool> AddOutlookAsync()
	{
		return true;
	}

	public async Task<bool> AddOtherAddressAsync()
	{
		return true;
	}

	public async Task ListLast50GmailEmailsAsync(UserCredential credential)
	{
		var service = new GmailService(new BaseClientService.Initializer()
		{
			HttpClientInitializer = credential,
			ApplicationName = "MailOAuthTester"
		});

		var request = service.Users.Messages.List("me");
		request.MaxResults = 50;
		request.LabelIds = "INBOX";
		request.IncludeSpamTrash = false;

		ListMessagesResponse response = await request.ExecuteAsync();

		if (response.Messages == null || response.Messages.Count == 0)
		{
			Console.WriteLine("Aucun message trouvé.");
			return;
		}

		Console.WriteLine("=== 50 derniers emails ===\n");

		foreach (var msg in response.Messages)
		{
			var fullMessage = await service.Users.Messages.Get("me", msg.Id).ExecuteAsync();

			string subject = fullMessage.Payload.Headers.FirstOrDefault(h => h.Name == "Subject")?.Value ?? "(Sans objet)";
			string from = fullMessage.Payload.Headers.FirstOrDefault(h => h.Name == "From")?.Value ?? "(Inconnu)";

			Console.WriteLine($"{from} | {subject}");

			string body = GetMessageBody(fullMessage);
			Console.WriteLine("---- CONTENU ----");
			Console.WriteLine(body);
			Console.WriteLine("-----------------\n");
		}
	}

	// Déconnexion
	public async Task<bool> RemoveGmailAccountAsync(AccountGmail account)
	{
		await _gmailLogoutService.LogoutAsync(account);
		await _addressRepository.DeleteAsync(account);

		return true;
	}

	public string GetMessageBody(Message message)
	{
		// Pas de multipart
		if (message.Payload.Body != null && !string.IsNullOrEmpty(message.Payload.Body.Data))
		{
			return DecodeBase64(message.Payload.Body.Data);
		}

		// Cas multipart
		if (message.Payload.Parts != null)
		{
			foreach (var part in message.Payload.Parts)
			{
				// Texte brut
				if (part.MimeType == "text/plain" && part.Body?.Data != null)
				{
					return DecodeBase64(part.Body.Data);
				}

				// Sinon HTML
				if (part.MimeType == "text/html" && part.Body?.Data != null)
				{
					return DecodeBase64(part.Body.Data);
				}
			}
		}

		return "(Contenu du message non trouvé)";
	}

	public string DecodeBase64(string input)
	{
		if (string.IsNullOrEmpty(input))
			return string.Empty;

		// Gmail utilise Base64 URL-safe
		input = input.Replace('-', '+').Replace('_', '/');
		var bytes = Convert.FromBase64String(input);
		return Encoding.UTF8.GetString(bytes);
	}
}
