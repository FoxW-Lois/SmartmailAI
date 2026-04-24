using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SmartmailAI.Core.Contracts.Services;

namespace SmartmailAI.Core.Services;

/// <summary>
/// Télécharge et met en cache la liste de domaines malveillants red.flag.domains.
/// En cas d'échec réseau, tente un fallback sur un fichier local.
/// La liste est rafraîchie automatiquement toutes les 24h.
/// </summary>
public class RedFlagDomainService : IRedFlagDomainService, IDisposable
{
	// -------------------------------------------------------------------------
	// Constantes
	// -------------------------------------------------------------------------

	private const string RemoteUrl = "https://dl.red.flag.domains/red.flag.domains.txt";
	private const string CacheFileName = "red.flag.domains.cache.txt";
	private static readonly TimeSpan RefreshInterval = TimeSpan.FromHours(24);

	// -------------------------------------------------------------------------
	// État interne
	// -------------------------------------------------------------------------

	private readonly HttpClient _http;
	private readonly string _cacheFilePath;
	private readonly SemaphoreSlim _lock = new(1, 1);

	private HashSet<string>? _domains;
	private DateTime _lastRefresh = DateTime.MinValue;

	// -------------------------------------------------------------------------
	// Constructeur
	// -------------------------------------------------------------------------

	public RedFlagDomainService()
	{
		_http = new HttpClient
		{
			Timeout = TimeSpan.FromSeconds(30)
		};

		// Cache local dans le dossier AppData de l'utilisateur
		var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		var appFolder = Path.Combine(appData, "SmartmailAI");
		Directory.CreateDirectory(appFolder);
		_cacheFilePath = Path.Combine(appFolder, CacheFileName);
	}

	// -------------------------------------------------------------------------
	// Interface publique
	// -------------------------------------------------------------------------

	public async Task<bool> IsFlaggedDomainAsync(string domain)
	{
		if (string.IsNullOrWhiteSpace(domain))
			return false;

		var domains = await GetDomainsAsync();
		return domains.Contains(domain.Trim().ToLowerInvariant());
	}

	public async Task RefreshAsync()
	{
		await LoadFromRemoteAsync(force: true);
	}

	// -------------------------------------------------------------------------
	// Logique interne
	// -------------------------------------------------------------------------

	private async Task<HashSet<string>> GetDomainsAsync()
	{
		// Déjà chargé et pas encore périmé → on retourne directement
		if (_domains is not null && DateTime.UtcNow - _lastRefresh < RefreshInterval)
			return _domains;

		await _lock.WaitAsync();
		try
		{
			// Double-check après acquisition du verrou
			if (_domains is not null && DateTime.UtcNow - _lastRefresh < RefreshInterval)
				return _domains;

			// 1. Essai réseau
			var loaded = await LoadFromRemoteAsync(force: false);

			// 2. Fallback fichier local si réseau indisponible
			if (!loaded && _domains is null)
				await LoadFromCacheAsync();

			// 3. Liste vide en dernier recours (ne bloque pas l'appli)
			_domains ??= [];

			return _domains;
		}
		finally
		{
			_lock.Release();
		}
	}

	/// <summary>
	/// Télécharge la liste depuis l'URL distante.
	/// Sauvegarde en cache local en cas de succès.
	/// </summary>
	private async Task<bool> LoadFromRemoteAsync(bool force)
	{
		if (!force && _domains is not null && DateTime.UtcNow - _lastRefresh < RefreshInterval)
			return true;

		try
		{
			var content = await _http.GetStringAsync(RemoteUrl);
			var parsed = ParseDomainList(content);

			_domains = parsed;
			_lastRefresh = DateTime.UtcNow;

			System.Diagnostics.Debug.WriteLine($"[RedFlagDomains] ✅ {_domains.Count} domaines chargés depuis le réseau.");

			// Sauvegarde du cache local (best-effort)
			await SaveCacheAsync(content);

			return true;
		}
		catch (Exception ex)
		{
			// Réseau indisponible ou erreur HTTP → on laisse le fallback prendre le relais
			System.Diagnostics.Debug.WriteLine($"[RedFlagDomains] ❌ Échec du chargement réseau : {ex.Message}");
			return false;
		}
	}

	/// <summary>
	/// Charge la liste depuis le fichier de cache local (fallback hors-ligne).
	/// </summary>
	private async Task LoadFromCacheAsync()
	{
		try
		{
			if (!File.Exists(_cacheFilePath))
				return;

			var content = await File.ReadAllTextAsync(_cacheFilePath);
			_domains = ParseDomainList(content);

			System.Diagnostics.Debug.WriteLine($"[RedFlagDomains] ⚠️ {_domains.Count} domaines chargés depuis le cache local (réseau indisponible).");

			// On ne met pas à jour _lastRefresh : le prochain démarrage
			// tentera à nouveau le réseau.
		}
		catch (Exception)
		{
			// Cache corrompu ou inaccessible → on continuera avec une liste vide
		}
	}

	/// <summary>
	/// Sauvegarde le contenu brut dans le fichier de cache local.
	/// </summary>
	private async Task SaveCacheAsync(string content)
	{
		try
		{
			await File.WriteAllTextAsync(_cacheFilePath, content);
		}
		catch (Exception)
		{
			// Écriture impossible (droits, espace disque…) → non bloquant
		}
	}

	/// <summary>
	/// Parse le fichier texte : ignore les commentaires (#) et les lignes vides.
	/// </summary>
	private static HashSet<string> ParseDomainList(string content) =>
		content
			.Split('\n', StringSplitOptions.RemoveEmptyEntries)
			.Select(line => line.Trim().ToLowerInvariant())
			.Where(line => line.Length > 0 && !line.StartsWith('#'))
			.ToHashSet();

	// -------------------------------------------------------------------------
	// Dispose
	// -------------------------------------------------------------------------

	public void Dispose()
	{
		_http.Dispose();
		_lock.Dispose();
	}
}
