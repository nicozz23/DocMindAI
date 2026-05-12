using System.Collections.Concurrent;
using Microsoft.SemanticKernel.ChatCompletion;
using ProyectoIA.Application.Interfaces;

namespace ProyectoIA.Infrastructure.Services;

/// <summary>
/// Implementación simple en memoria para el historial de chat.
/// En un entorno real, esto podría usar Redis o una base de datos.
/// </summary>
public class InMemoryHistoryService : IHistoryService
{
    // Usamos ConcurrentDictionary por si hay múltiples hilos accediendo
    private readonly ConcurrentDictionary<string, ChatHistory> _histories = new();
    private readonly ConcurrentDictionary<string, ChatSessionMetadata> _metadata = new();

    public ChatHistory GetHistory(string sessionId)
    {
        // Al crear una historia, también inicializamos su metadata si no existe
        if (!_metadata.ContainsKey(sessionId))
        {
            _metadata.TryAdd(sessionId, new ChatSessionMetadata { Id = sessionId });
        }
        return _histories.GetOrAdd(sessionId, _ => new ChatHistory());
    }

    public void AddUserMessage(string sessionId, string message)
    {
        var history = GetHistory(sessionId);
        history.AddUserMessage(message);
        
        // Actualizar fecha de último mensaje
        if (_metadata.TryGetValue(sessionId, out var meta))
        {
            meta.LastUpdate = DateTime.Now;
        }
    }

    public void AddAssistantMessage(string sessionId, string message)
    {
        var history = GetHistory(sessionId);
        history.AddAssistantMessage(message);
    }

    public void ClearHistory(string sessionId)
    {
        _histories.TryRemove(sessionId, out _);
        _metadata.TryRemove(sessionId, out _);
    }

    public IEnumerable<ChatSessionMetadata> GetAllSessions()
    {
        return _metadata.Values.OrderByDescending(x => x.LastUpdate);
    }

    public void SetSessionTitle(string sessionId, string title)
    {
        if (_metadata.TryGetValue(sessionId, out var meta))
        {
            meta.Title = title;
        }
    }
}
