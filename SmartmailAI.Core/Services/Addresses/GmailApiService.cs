using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using SmartmailAI.Core.Contracts.Services.Addresses;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Services.Addresses;

public class GmailApiService : IGmailApiService
{
	public async Task<string> GetEmailAddressAsync(UserCredential credential)
	{
		var service = new GmailService(new BaseClientService.Initializer
		{
			HttpClientInitializer = credential,
			ApplicationName = "MailOAuthTester"
		});

		var profile = await service.Users.GetProfile("me").ExecuteAsync();
		return profile.EmailAddress;
	}

	public async Task<List<EmailGmail>> GetLastMessagesAsync(UserCredential credential, string MailboxType, bool isAddingNewAddress, int? maxResults = 50,
		DateTime? lastConnection = null)
	{
		var service = new GmailService(new BaseClientService.Initializer
		{
			HttpClientInitializer = credential,
			ApplicationName = "SmartmailAI"
		});

		var request = service.Users.Messages.List("me");
		request.MaxResults = maxResults;
		request.LabelIds = MailboxType.ToUpper();
		request.IncludeSpamTrash = false;

		if (lastConnection is not null && !isAddingNewAddress)
		{
			var unixSeconds = ToUnixSeconds(lastConnection.Value);
			request.Q = $"after:{unixSeconds}";
		}

		var response = await request.ExecuteAsync();

		if (response.Messages == null)
			return [];

		var result = new List<EmailGmail>();
		string emailAddressOwner = await GetEmailAddressAsync(credential);

		foreach (var msg in response.Messages)
		{
			var full = await service.Users.Messages.Get("me", msg.Id).ExecuteAsync();

			var (fromName, fromEmail) = ParseEmailAddress(GetHeader(full, "From"));
			var (toName, toEmail) = ParseEmailAddress(GetHeader(full, "To"));

			result.Add(new EmailGmail
			{
				Guid = msg.Id,
				FromEmail = fromEmail,
				FromName = fromName,
				ToEmail = toEmail,
				ToName = toName,
				Subject = GetHeader(full, "Subject"),
				Body = GetMessageBody(full),
				Date = GetMessageDate(full),
				Owner = emailAddressOwner,
				MailboxType = MailboxType
			});
		}

		return result;
	}

	// Helpers internes au Service
	private static string GetHeader(Message msg, string name) =>
		msg.Payload.Headers.FirstOrDefault(h => h.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty;

	private static string GetMessageBody(Message message)
	{
		if (!string.IsNullOrEmpty(message.Payload.Body?.Data))
			return DecodeBase64(message.Payload.Body.Data);

		if (message.Payload.Parts != null)
		{
			foreach (var part in message.Payload.Parts)
			{
				if (part.MimeType is "text/plain" or "text/html")
				{
					if (!string.IsNullOrEmpty(part.Body?.Data))
						return DecodeBase64(part.Body.Data);
				}
			}
		}

		return string.Empty;
	}

	private static DateTime? GetMessageDate(Message msg)
	{
		return msg.InternalDate is not null ?
			DateTimeOffset.FromUnixTimeMilliseconds(msg.InternalDate!.Value).LocalDateTime : DateTime.UtcNow;
	}

	private static string DecodeBase64(string input)
	{
		input = input.Replace('-', '+').Replace('_', '/');
		var bytes = Convert.FromBase64String(input);
		return Encoding.UTF8.GetString(bytes);
	}

	private static long ToUnixSeconds(DateTime dateTime)
	{
		return new DateTimeOffset(dateTime).ToUnixTimeSeconds();
	}

	private static (string? DisplayName, string Email) ParseEmailAddress(string? rawHeader)
	{
		if (string.IsNullOrWhiteSpace(rawHeader))
			return (null, string.Empty);

		try
		{
			var address = new MailAddress(rawHeader);
			return (string.IsNullOrWhiteSpace(address.DisplayName) ? null : address.DisplayName, address.Address);
		}
		catch
		{
			// Fallback si format non standard
			return (null, rawHeader);
		}
	}
}
