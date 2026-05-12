using System.Threading;
using System.Threading.Tasks;

using ProyectoIA.Application.DTOs;
using System.Collections.Generic;

namespace ProyectoIA.Application.Interfaces;

public interface IChatService
{
    Task<ChatReply> GetReplyAsync(string message, string sessionId = "default", CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> GetStreamingReplyAsync(string message, string sessionId = "default", CancellationToken cancellationToken = default);
}
