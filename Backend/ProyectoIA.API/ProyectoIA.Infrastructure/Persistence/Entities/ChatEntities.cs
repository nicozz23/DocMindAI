using System;
using System.Collections.Generic;

namespace ProyectoIA.Infrastructure.Persistence.Entities;

public class ChatSessionEntity
{
    public string Id { get; set; } = string.Empty; // SessionId del chat
    public string Title { get; set; } = "Nueva Conversación";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
    
    // Relación con los mensajes
    public List<ChatMessageEntity> Messages { get; set; } = new();
}

public class ChatMessageEntity
{
    public int Id { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // User, Assistant, System
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Relación inversa
    public ChatSessionEntity Session { get; set; } = null!;
}
