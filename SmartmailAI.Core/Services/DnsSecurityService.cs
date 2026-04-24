using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using SmartmailAI.Core.Contracts.Services;

namespace SmartmailAI.Core.Services;

/// <summary>
/// Vérifie les enregistrements SPF et DMARC d'un domaine via DNS over HTTPS (DoH).
/// Utilise l'API Cloudflare DoH (1.1.1.1) — pas de dépendance externe, pas de clé API.
/// 
/// Pourquoi DoH plutôt que System.Net.Dns ?
/// → System.Net.Dns ne supporte pas les requêtes TXT en .NET sur Windows packagé (MSIX).
/// → DoH via HTTPS fonctionne dans tous les contextes, y compris les apps packagées.
/// </summary>
public class DnsSecurityService : IDnsSecurityService, IDisposable
{
	// -------------------------------------------------------------------------
	// Constantes
	// -------------------------------------------------------------------------

	private const string DoHUrl = "https://cloudflare-dns.com/dns-query";

	// -------------------------------------------------------------------------
	// État interne
	// -------------------------------------------------------------------------

	private readonly HttpClient _http;

	// Cache mémoire pour éviter de re-interroger le DNS pour le même domaine
	private readonly Dictionary<string, DnsSecurityResult> _cache = new();

	// -------------------------------------------------------------------------
	// Constructeur
	// -------------------------------------------------------------------------

	public DnsSecurityService()
	{
		_http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
		_http.DefaultRequestHeaders.Add("Accept", "application/dns-json");
	}

	// -------------------------------------------------------------------------
	// Interface publique
	// -------------------------------------------------------------------------

	public async Task<DnsSecurityResult> CheckDomainAsync(string senderEmail)
	{
		if (string.IsNullOrWhiteSpace(senderEmail) || !senderEmail.Contains('@'))
			return BuildUnknownResult("invalid");

		var domain = senderEmail.Split('@')[1].Trim().ToLowerInvariant();

		if (_cache.TryGetValue(domain, out var cached))
			return cached;

		var result = await CheckDomainInternalAsync(domain);
		_cache[domain] = result;

		System.Diagnostics.Debug.WriteLine(
			$"[DNS] {domain} → SPF={result.SpfStatus}, DMARC={result.DmarcStatus}, Suspicious={result.IsSuspicious}");

		return result;
	}

	// -------------------------------------------------------------------------
	// Logique interne
	// -------------------------------------------------------------------------

	private async Task<DnsSecurityResult> CheckDomainInternalAsync(string domain)
	{
		// Requêtes SPF et DMARC en parallèle
		var spfTask   = QueryTxtAsync(domain);
		var dmarcTask = QueryTxtAsync($"_dmarc.{domain}");

		await Task.WhenAll(spfTask, dmarcTask);

		var spfRecords   = spfTask.Result;
		var dmarcRecords = dmarcTask.Result;

		// --- Analyse SPF ---
		var spfRecord = spfRecords.FirstOrDefault(r =>
			r.StartsWith("v=spf1", StringComparison.OrdinalIgnoreCase));

		SpfStatus spfStatus;
		if (spfRecord is null)
		{
			spfStatus = SpfStatus.None;
		}
		else if (spfRecord.Contains("-all", StringComparison.OrdinalIgnoreCase))
		{
			spfStatus = SpfStatus.Fail; // Politique stricte → les faux expéditeurs échouent
		}
		else if (spfRecord.Contains("~all", StringComparison.OrdinalIgnoreCase))
		{
			spfStatus = SpfStatus.SoftFail; // Politique permissive → suspect
		}
		else
		{
			spfStatus = SpfStatus.Pass;
		}

		// --- Analyse DMARC ---
		var dmarcRecord = dmarcRecords.FirstOrDefault(r =>
			r.StartsWith("v=DMARC1", StringComparison.OrdinalIgnoreCase));

		var dmarcStatus = dmarcRecord is not null ? DmarcStatus.Present : DmarcStatus.None;

		// --- Évaluation globale ---
		bool isSuspicious = spfStatus == SpfStatus.None || dmarcStatus == DmarcStatus.None;
		string? warning   = BuildWarning(spfStatus, dmarcStatus);

		return new DnsSecurityResult(
			Domain:      domain,
			SpfStatus:   spfStatus,
			SpfRecord:   spfRecord,
			DmarcStatus: dmarcStatus,
			DmarcRecord: dmarcRecord,
			IsSuspicious: isSuspicious,
			Warning:     warning
		);
	}

	/// <summary>
	/// Interroge les enregistrements TXT d'un domaine via Cloudflare DoH.
	/// </summary>
	private async Task<List<string>> QueryTxtAsync(string name)
	{
		try
		{
			var url = $"{DoHUrl}?name={Uri.EscapeDataString(name)}&type=TXT";
			var json = await _http.GetStringAsync(url);

			using var doc = JsonDocument.Parse(json);

			// Status 0 = NOERROR, 3 = NXDOMAIN (domaine inexistant)
			if (!doc.RootElement.TryGetProperty("Status", out var status) || status.GetInt32() != 0)
				return [];

			if (!doc.RootElement.TryGetProperty("Answer", out var answers))
				return [];

			var records = new List<string>();
			foreach (var answer in answers.EnumerateArray())
			{
				// Type 16 = TXT
				if (answer.TryGetProperty("type", out var type) && type.GetInt32() == 16 &&
					answer.TryGetProperty("data", out var data))
				{
					// Cloudflare DoH entoure les valeurs de guillemets → les retirer
					var value = data.GetString()?.Trim('"') ?? string.Empty;
					if (!string.IsNullOrWhiteSpace(value))
						records.Add(value);
				}
			}

			return records;
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[DNS] ❌ Erreur DoH pour '{name}' : {ex.Message}");
			return [];
		}
	}

	private static string? BuildWarning(SpfStatus spf, DmarcStatus dmarc)
	{
		var issues = new List<string>();

		if (spf == SpfStatus.None)
			issues.Add("aucun enregistrement SPF");
		else if (spf == SpfStatus.SoftFail)
			issues.Add("SPF en mode permissif (~all)");

		if (dmarc == DmarcStatus.None)
			issues.Add("aucune politique DMARC");

		return issues.Count > 0
			? $"Configuration DNS suspecte : {string.Join(", ", issues)}."
			: null;
	}

	private static DnsSecurityResult BuildUnknownResult(string domain) =>
		new(domain, SpfStatus.Unknown, null, DmarcStatus.Unknown, null, false, null);

	// -------------------------------------------------------------------------
	// Dispose
	// -------------------------------------------------------------------------

	public void Dispose() => _http.Dispose();
}
