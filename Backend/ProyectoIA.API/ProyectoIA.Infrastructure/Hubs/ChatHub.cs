using Microsoft.AspNetCore.SignalR;
using ProyectoIA.Application.Interfaces;
using System.Threading.Tasks;

namespace ProyectoIA.Infrastructure.Hubs;

public class ChatHub : Hub
{
    private readonly IChatService _chatService;

    public ChatHub(IChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task SendMessageStream(string message, string sessionId)
    {
        await foreach (var chunk in _chatService.GetStreamingReplyAsync(message, sessionId))
        {
            await Clients.Caller.SendAsync("ReceiveChatChunk", chunk);
        }
        
        await Clients.Caller.SendAsync("ChatStreamFinished");
    }
}
