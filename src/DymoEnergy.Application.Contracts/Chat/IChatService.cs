using System.Threading.Tasks;

namespace DymoEnergy.Chat;

public interface IChatService
{
    Task<ChatResponse> SendAsync(ChatRequest request);
}
