using System;
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

	private readonly SemaphoreSlim _startLock = new(1, 1);
	private bool _isRunning;

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
		await _startLock.WaitAsync();
		try
		{
			if (_isRunning)
				return;

			_isRunning = true;

			_cts = new CancellationTokenSource();

			await RunAsync();
		}
		finally
		{
			_startLock.Release();
		}
	}

	public async Task RunAsync()
	{
		try
		{
			// Ne JAMAIS casser le while
			while (await _timer.WaitForNextTickAsync(_cts.Token))
			{
				var addressRefreshList = await _addressesRepository.GetAllAddressAsync();

				if (addressRefreshList is null || addressRefreshList.Count == 0) continue;

				foreach (var address in addressRefreshList)
				{
					if (_cts.IsCancellationRequested)
						return;

					await SyncNewEmailsAsync(address!);
				}

				await _authService.UpdateLastConnection();
			}
		}
		catch (OperationCanceledException)
		{
			// Arrêt pour éviter les crashs et la mobilisation inutile de RAM
			Stop();
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Erreur inattendue dans la boucle de synchro Gmail: {ex}");
			_isRunning = false;
		}
	}

	public async Task SyncNewEmailsAsync(AccountGmail accountGmail)
	{
		var mails = await _mailReaderService.GetLastMessagesFromAccountAsync(accountGmail, false);

		foreach (var email in mails)
			await _emailRepository.AddAsync(email);
	}

	public void Stop()
	{
		if (!_isRunning)
			return;

		_cts.Cancel();
		_isRunning = false;
	}

	public async ValueTask DisposeAsync()
	{
		_cts.Cancel();
		_timer.Dispose();
	}
}
