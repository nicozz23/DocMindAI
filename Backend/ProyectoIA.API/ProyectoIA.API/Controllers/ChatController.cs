using Microsoft.AspNetCore.Mvc;
using ProyectoIA.Application.Interfaces;
using System.Threading.Tasks;

namespace ProyectoIA.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    // Un DTO simple para recibir el mensaje
    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public string SessionId { get; set; } = "default";
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ChatRequest request, [FromServices] IHistoryService historyService)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest("El mensaje no puede estar vacío.");

        // Obtenemos el objeto completo con la respuesta y las fuentes
        var chatReply = await _chatService.GetReplyAsync(request.Message, request.SessionId);
        
        // Si el chat es nuevo o no tiene título real, generamos uno
        var history = historyService.GetHistory(request.SessionId);
        if (history.Count <= 3) 
        {
             string title = request.Message.Length > 30 ? request.Message.Substring(0, 30) + "..." : request.Message;
             historyService.SetSessionTitle(request.SessionId, title);
        }

        return Ok(chatReply);
    }

    [HttpGet("sessions")]
    public IActionResult GetSessions([FromServices] IHistoryService historyService)
    {
        return Ok(historyService.GetAllSessions());
    }

    [HttpGet("history/{sessionId}")]
    public IActionResult GetHistory(string sessionId, [FromServices] IHistoryService historyService)
    {
        var history = historyService.GetHistory(sessionId);
        var messages = history.Select(m => new { 
            Role = m.Role.ToString(), 
            Content = m.Content 
        }).Where(m => m.Role != "System"); // No enviamos el System Prompt al front
        
        return Ok(messages);
    }

    [HttpDelete("history/{sessionId}")]
    public IActionResult ClearHistory(string sessionId, [FromServices] IHistoryService historyService)
    {
        historyService.ClearHistory(sessionId);
        return Ok(new { Message = "Historial de chat reiniciado correctamente." });
    }
}
