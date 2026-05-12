using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using ProyectoIA.Application.Interfaces;
using ProyectoIA.Infrastructure.Persistence;
using ProyectoIA.Infrastructure.Persistence.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProyectoIA.Infrastructure.Services;

public class SqlHistoryService : IHistoryService
{
    private readonly ApplicationDbContext _context;

    public SqlHistoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public ChatHistory GetHistory(string sessionId)
    {
        var session = _context.Sessions
            .Include(s => s.Messages)
            .AsNoTracking() // Mejora rendimiento para lectura
            .FirstOrDefault(s => s.Id == sessionId);

        var history = new ChatHistory();
        if (session != null)
        {
            foreach (var msg in session.Messages.OrderBy(m => m.Timestamp))
            {
                if (msg.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
                    history.AddUserMessage(msg.Content);
                else if (msg.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
                    history.AddAssistantMessage(msg.Content);
                else
                    history.AddSystemMessage(msg.Content);
            }
        }

        return history;
    }

    public void AddUserMessage(string sessionId, string message)
    {
        AddMessage(sessionId, "User", message);
    }

    public void AddAssistantMessage(string sessionId, string message)
    {
        AddMessage(sessionId, "Assistant", message);
    }

    public void ClearHistory(string sessionId)
    {
        var messages = _context.Messages.Where(m => m.SessionId == sessionId);
        _context.Messages.RemoveRange(messages);
        _context.SaveChanges();
    }

    public IEnumerable<ChatSessionMetadata> GetAllSessions()
    {
        return _context.Sessions
            .OrderByDescending(s => s.LastUpdate)
            .Select(s => new ChatSessionMetadata {
                Id = s.Id,
                Title = s.Title,
                LastUpdate = s.LastUpdate
            })
            .ToList();
    }

    public void SetSessionTitle(string sessionId, string title)
    {
        var session = _context.Sessions.Find(sessionId);
        if (session != null)
        {
            session.Title = title;
            _context.SaveChanges();
        }
    }

    private void AddMessage(string sessionId, string role, string content)
    {
        var session = _context.Sessions.FirstOrDefault(s => s.Id == sessionId);
        
        if (session == null)
        {
            session = new ChatSessionEntity 
            { 
                Id = sessionId,
                Title = role == "User" ? (content.Length > 30 ? content.Substring(0, 30) + "..." : content) : "Nueva Conversación"
            };
            _context.Sessions.Add(session);
        }
        else if (role == "User" && (session.Title == "Nueva Conversación" || string.IsNullOrEmpty(session.Title)))
        {
            // Actualizar título con el primer mensaje del usuario
            session.Title = content.Length > 30 ? content.Substring(0, 30) + "..." : content;
        }

        session.LastUpdate = DateTime.UtcNow;
        
        var msg = new ChatMessageEntity
        {
            SessionId = sessionId,
            Role = role,
            Content = content,
            Timestamp = DateTime.UtcNow
        };

        _context.Messages.Add(msg);
        _context.SaveChanges();
    }
}
