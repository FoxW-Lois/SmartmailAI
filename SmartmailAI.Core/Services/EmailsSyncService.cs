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
	private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);
	private CancellationTokenSource _cts = new();
	private Task? _runningTask;

	private readonly SemaphoreSlim _startLock = new(1, 1);
	private bool _isRunning;

	public EmailsSyncService(IMailReaderService mailReaderService, IEmailRepository emailRepository, IAddressesRepository addressesRepository,
		IAuthService authService)
	{
		_mailReaderService = mailReaderService;
		_emailRepository = emailRepository;
		_addressesRepository = addressesRepository;
		_authService = authService;
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

			_runningTask = Task.Run(RunAsync);
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
			using var timer = new PeriodicTimer(_interval);

			while (await timer.WaitForNextTickAsync(_cts.Token))
			{
				var addressRefreshList = await _addressesRepository.GetAllAddressesAsync();

				if (addressRefreshList is null || addressRefreshList.Count == 0) continue;

				foreach (var address in addressRefreshList)
				{
					if (_cts.IsCancellationRequested)
						return;

					await SyncNewEmailsAsync(address);
				}

				await _authService.UpdateLastConnection();
			}
		}
		catch (OperationCanceledException)
		{
			Debug.WriteLine("Synchronisation arrêtée.");
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Erreur inattendue dans la boucle de synchro des emails : {ex}");
		}
		finally
		{
			_isRunning = false;
		}
	}

	public async Task SyncNewEmailsAsync(AccountMailBase account)
	{
		if (account == null)
			return;

		var mails = await _mailReaderService.GetLastMessagesFromAccountAsync(false, account);

		foreach (var email in mails)
			await _emailRepository.AddEmailAsync(email);
	}

	public void Stop()
	{
		if (!_isRunning)
			return;

		_cts.Cancel();
		_isRunning = false;
		_runningTask = null;
	}

	public async ValueTask DisposeAsync()
	{
		Stop();

		if (_runningTask != null)
			await _runningTask;

		_cts.Dispose();
		_startLock.Dispose();
	}
}
