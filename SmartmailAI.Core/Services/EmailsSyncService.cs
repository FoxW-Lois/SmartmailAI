using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SmartmailAI.Core.Contracts.Repository;
using SmartmailAI.Core.Contracts.Services;
using SmartmailAI.Core.Contracts.Services.Addresses;
using SmartmailAI.Core.Contracts.Services.Authentication;
using SmartmailAI.Core.Models;

namespace SmartmailAI.Core.Services;

public class EmailsSyncService : IEmailsSyncService, IAsyncDisposable
{
	private readonly IMailReaderService _mailReaderService;
	private readonly IEmailRepository _emailRepository;
	private readonly IAddressesRepository _addressesRepository;
	private readonly IAuthService _authService;
	private readonly PeriodicTimer _timer;
	private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);
	private CancellationTokenSource _cts = new();


	public EmailsSyncService(IMailReaderService mailReaderService, IEmailRepository emailRepository, IAddressesRepository addressesRepository,
		IAuthService authService)
	{
		_mailReaderService = mailReaderService;
		_emailRepository = emailRepository;
		_addressesRepository = addressesRepository;
		_authService = authService;

		_timer = new PeriodicTimer(_interval);
	}

	public async Task StartAsync()
	{
		_cts = new CancellationTokenSource();
		var addressRefreshList = await _addressesRepository.GetAllAddressAsync();

		try
		{
			while (await _timer.WaitForNextTickAsync(_cts.Token))
			{
				try
				{
					foreach (var addressRefresh in addressRefreshList)
						await SyncNewEmailsAsync(addressRefresh!);
				}
				catch (Exception ex)
				{
					// Log l'erreur, ne pas casser la boucle
					Debug.WriteLine($"Erreur lors de la synchro Gmail: {ex}");
				}
			}
		}
		catch (OperationCanceledException)
		{
			// Arrêt propre
			Stop();
		}
	}

	public async Task SyncNewEmailsAsync(AccountGmail accountGmail)
	{
		var mails = await _mailReaderService.GetLastMessagesFromAccountAsync(accountGmail);

		foreach (var email in mails)
			await _emailRepository.AddAsync(email);

		await _authService.UpdateLastConnection();
	}

	public void Stop()
	{
		_cts.Cancel();
	}

	public async ValueTask DisposeAsync()
	{
		_cts.Cancel();
		_timer.Dispose();
	}
}
