using System.Collections.ObjectModel;
using System.Threading.Tasks;
using SmartmailAI.Core.Models.AI;

namespace SmartmailAI.Core.Contracts.Services;

public interface I_AIService
{
	Task<object> AIConversationAsync(ObservableCollection<AIMessage> Conversation);

	Task<string> AIRequestAsync(object request);
}
