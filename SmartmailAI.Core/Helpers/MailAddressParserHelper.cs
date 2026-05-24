using System;
using System.Collections.Generic;
using System.Linq;
using MimeKit;

namespace SmartmailAI.Core.Helpers;

public static class MailAddressParserHelper
{
	public static IEnumerable<MailboxAddress> ParseAddresses(string? input)
	{
		if (string.IsNullOrWhiteSpace(input))
			return [];

		return input
			.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Select(MailboxAddress.Parse);
	}

	public static IEnumerable<string> ParseStringAddresses(string? input)
	{
		if (string.IsNullOrWhiteSpace(input))
			return [];

		return input.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
	}

	public static string FormatStringAddresses(IEnumerable<string>? addresses)
	{
		return string.Join(", ", addresses ?? []);
	}
}
