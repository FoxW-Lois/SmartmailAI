using System.Collections.Generic;
using System.Threading.Tasks;
using SmartmailAI.Core.Models.Security;

namespace SmartmailAI.Core.Contracts.Repository;

public interface IMLDA_Repository
{
	Task<List<ManualLegitDomainsAndAddresses>?> GetAllMLDA_Async();

	Task<bool> MLDAExistsAsync(string mldaValue);

	Task AddMLDA_Async(ManualLegitDomainsAndAddresses mlda);

	Task UpdateMLDA_Async(ManualLegitDomainsAndAddresses mlda);
}
