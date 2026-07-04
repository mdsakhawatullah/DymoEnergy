using System.Threading.Tasks;
using DymoEnergy.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DymoEnergy.Controllers.Chat;

[AllowAnonymous]
[Route("api/chat")]
public class ChatController : DymoEnergyController
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpPost]
    public async Task<ChatResponse> PostAsync([FromBody] ChatRequest request)
    {
        return await _chatService.SendAsync(request);
    }
}
