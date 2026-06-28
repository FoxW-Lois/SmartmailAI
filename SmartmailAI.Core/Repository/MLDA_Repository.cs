using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartmailAI.Core.AppDbContext;
using SmartmailAI.Core.Contracts.Repository;
using SmartmailAI.Core.Contracts.Services.LocalSecurity;
using SmartmailAI.Core.Models.Security;

namespace SmartmailAI.Core.Repository;

public class MLDA_Repository(IDbContextFactory<AppDbContext_MLDA> factory, IAesService aesService) : IMLDA_Repository
{
	private readonly IDbContextFactory<AppDbContext_MLDA> _factory = factory;
	private readonly IAesService _aesService = aesService;

	public async Task<List<ManualLegitDomainsAndAddresses>?> GetAllMLDA_Async()
	{
		using var _context = _factory.CreateDbContext();

		var mldaList = await _context.ManualLegitDomainsAndAddresses.ToListAsync();

		return mldaList;
	}

	public async Task<bool> MLDAExistsAsync(string mldaValue)
	{
		using var _context = _factory.CreateDbContext();

		return await _context.ManualLegitDomainsAndAddresses
			.AnyAsync(a => a.Value == mldaValue);
	}

	public async Task AddMLDA_Async(ManualLegitDomainsAndAddresses mlda)
	{
		using var _context = _factory.CreateDbContext();

		_context.ManualLegitDomainsAndAddresses.Add(mlda);
		await _context.SaveChangesAsync();
	}

	public async Task UpdateMLDA_Async(ManualLegitDomainsAndAddresses mlda)
	{
		using var _context = _factory.CreateDbContext();

		_context.ManualLegitDomainsAndAddresses.Update(mlda);
		await _context.SaveChangesAsync();
	}
}
