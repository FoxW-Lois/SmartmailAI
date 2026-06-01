using System.Threading.Tasks;

namespace SmartmailAI.Core.Contracts;

public interface IEncryptDecryptDatas<T>
{
	Task<T> EncryptDataAsync(T data);

	Task<T> DecryptDataAsync(T data);
}
