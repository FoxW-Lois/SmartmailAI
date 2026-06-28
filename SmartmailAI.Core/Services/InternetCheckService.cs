using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SmartmailAI.Core.Services;

public class InternetCheckService
{
	private static readonly HttpClient HttpClient = new()
	{
		Timeout = TimeSpan.FromSeconds(3)
	};

	public static async Task<bool> HasInternetConnectionAsync()
	{
		try
		{
			using var response = await HttpClient.GetAsync("https://clients3.google.com/generate_204",
				HttpCompletionOption.ResponseHeadersRead);

			return response.IsSuccessStatusCode;
		}
		catch
		{
			return false;
		}
	}
}
