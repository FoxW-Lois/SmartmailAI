using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SmartmailAI.Core.Contracts.Services;

namespace SmartmailAI.Core.Services;

/// <summary>
/// Vérifie les pièces jointes via l'API VirusTotal v3.
/// La recherche se fait par nom de fichier normalisé (sans upload du fichier).
/// Nécessite une clé API VirusTotal (gratuite sur https://www.virustotal.com).
/// </summary>
public class VirusTotalService : IVirusTotalService, IDisposable
{
	// -------------------------------------------------------------------------
	// Constantes
	// -------------------------------------------------------------------------

	private const string BaseUrl = "https://www.virustotal.com/api/v3/";

	// Extensions qui méritent toujours une vérification, même sans API
	private static readonly string[] _dangerousExtensions =
	[
		".exe", ".scr", ".bat", ".cmd", ".com", ".pif",
		".js",  ".jse", ".vbs", ".vbe", ".wsf", ".wsh",
		".ps1", ".psm1", ".msi", ".dll", ".sys",
		".zip", ".rar", ".7z", ".iso", ".img",
		".docm", ".xlsm", ".pptm",           // Office avec macros
		".lnk", ".url",                        // Raccourcis
		".hta", ".htm", ".html"               // HTML potentiellement malveillant
	];

	// -------------------------------------------------------------------------
	// État interne
	// -------------------------------------------------------------------------

	private readonly HttpClient _http;
	private readonly string? _apiKey;

	// Cache simple pour éviter de re-interroger VirusTotal pour le même fichier
	private readonly Dictionary<string, VirusTotalResult> _cache = new();

	// -------------------------------------------------------------------------
	// Constructeur
	// -------------------------------------------------------------------------

	public VirusTotalService(string? apiKey = null)
	{
		_apiKey = apiKey;
		_http = new HttpClient
		{
			BaseAddress = new Uri(BaseUrl),
			Timeout = TimeSpan.FromSeconds(15)
		};

		if (!string.IsNullOrWhiteSpace(_apiKey))
			_http.DefaultRequestHeaders.Add("x-apikey", _apiKey);
	}

	// -------------------------------------------------------------------------
	// Interface publique
	// -------------------------------------------------------------------------

	public async Task<IReadOnlyList<VirusTotalResult>> AnalyzeAttachmentsAsync(IList<string> fileNames)
	{
		if (fileNames is null || fileNames.Count == 0)
			return [];

		var results = new List<VirusTotalResult>();

		foreach (var fileName in fileNames)
		{
			if (string.IsNullOrWhiteSpace(fileName))
				continue;

			// Filtre : on n'interroge VirusTotal que pour les extensions suspectes
			if (!IsSuspiciousExtension(fileName))
				continue;

			var result = await AnalyzeSingleFileAsync(fileName);
			if (result is not null)
				results.Add(result);
		}

		return results;
	}

	// -------------------------------------------------------------------------
	// Logique interne
	// -------------------------------------------------------------------------

	private async Task<VirusTotalResult?> AnalyzeSingleFileAsync(string fileName)
	{
		// Cache hit
		if (_cache.TryGetValue(fileName.ToLowerInvariant(), out var cached))
			return cached;

		// Sans clé API → analyse locale uniquement (extension dangereuse)
		if (string.IsNullOrWhiteSpace(_apiKey))
			return BuildLocalResult(fileName);

		try
		{
			// Recherche par nom de fichier sur VirusTotal
			// L'API /files/search permet de chercher par métadonnées sans uploader
			var query = Uri.EscapeDataString($"name:{fileName}");
			var response = await _http.GetAsync($"intelligence/search?query={query}&limit=1");

			if (!response.IsSuccessStatusCode)
			{
				System.Diagnostics.Debug.WriteLine($"[VirusTotal] ⚠️ Réponse HTTP {response.StatusCode} pour '{fileName}'");
				return BuildLocalResult(fileName);
			}

			var json = await response.Content.ReadAsStringAsync();
			var result = ParseSearchResponse(fileName, json);

			if (result is not null)
				_cache[fileName.ToLowerInvariant()] = result;

			return result ?? BuildLocalResult(fileName);
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[VirusTotal] ❌ Erreur pour '{fileName}' : {ex.Message}");
			return BuildLocalResult(fileName);
		}
	}

	/// <summary>
	/// Parse la réponse JSON de l'API VirusTotal /intelligence/search.
	/// </summary>
	private static VirusTotalResult? ParseSearchResponse(string fileName, string json)
	{
		try
		{
			using var doc = JsonDocument.Parse(json);
			var data = doc.RootElement.GetProperty("data");

			if (data.GetArrayLength() == 0)
				return null;

			var file = data[0];
			var attributes = file.GetProperty("attributes");
			var stats = attributes.GetProperty("last_analysis_stats");

			int malicious  = stats.TryGetProperty("malicious",  out var m) ? m.GetInt32() : 0;
			int suspicious = stats.TryGetProperty("suspicious", out var s) ? s.GetInt32() : 0;
			int total      = stats.TryGetProperty("harmless",   out var h) ? h.GetInt32() : 0
				+ malicious + suspicious
				+ (stats.TryGetProperty("undetected", out var u) ? u.GetInt32() : 0);

			string? permalink = file.TryGetProperty("id", out var id)
				? $"https://www.virustotal.com/gui/file/{id.GetString()}"
				: null;

			return new VirusTotalResult(
				FileName:       fileName,
				IsMalicious:    malicious + suspicious > 0,
				MaliciousCount: malicious + suspicious,
				TotalEngines:   total,
				Permalink:      permalink
			);
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[VirusTotal] ❌ Erreur parsing JSON : {ex.Message}");
			return null;
		}
	}

	/// <summary>
	/// Résultat local basé uniquement sur l'extension (fallback sans API).
	/// </summary>
	private static VirusTotalResult BuildLocalResult(string fileName) =>
		new(
			FileName:       fileName,
			IsMalicious:    true,
			MaliciousCount: -1,       // -1 = analyse locale uniquement
			TotalEngines:   0,
			Permalink:      null
		);

	private static bool IsSuspiciousExtension(string fileName) =>
		_dangerousExtensions.Any(ext =>
			fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase));

	// -------------------------------------------------------------------------
	// Dispose
	// -------------------------------------------------------------------------

	public void Dispose() => _http.Dispose();
}
