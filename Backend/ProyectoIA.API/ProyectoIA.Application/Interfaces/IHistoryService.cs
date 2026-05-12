using Microsoft.SemanticKernel.ChatCompletion;

namespace ProyectoIA.Application.Interfaces;

public interface IHistoryService
{
    ChatHistory GetHistory(string sessionId);
    void AddUserMessage(string sessionId, string message);
    void AddAssistantMessage(string sessionId, string message);
    void ClearHistory(string sessionId);
    
    // Nuevos métodos para la gestión de múltiples chats
    IEnumerable<ChatSessionMetadata> GetAllSessions();
    void SetSessionTitle(string sessionId, string title);
}

public class ChatSessionMetadata
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = "Nueva Conversación";
    public DateTime LastUpdate { get; set; } = DateTime.Now;
}
