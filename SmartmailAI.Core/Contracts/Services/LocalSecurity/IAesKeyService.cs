using System.Threading.Tasks;

namespace SmartmailAI.Core.Contracts.Services.LocalSecurity;

public interface IAesKeyService
{
	Task<byte[]> GetOrCreateKeyAsync();
}
