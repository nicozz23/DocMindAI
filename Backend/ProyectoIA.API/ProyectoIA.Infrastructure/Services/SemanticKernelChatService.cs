using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;
using ProyectoIA.Application.Interfaces;
using ProyectoIA.Application.DTOs;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ProyectoIA.Infrastructure.Services;

/// <summary>
/// Servicio de Chat que implementa RAG (Retrieval-Augmented Generation).
/// </summary>
public class SemanticKernelChatService : IChatService
{
    private readonly IChatCompletionService _chatCompletionService;
#pragma warning disable SKEXP0001
    private readonly ITextEmbeddingGenerationService _embeddingService;
#pragma warning restore SKEXP0001
#pragma warning disable SKEXP0026
    private readonly IVectorStore _vectorStore;
    private readonly IHistoryService _historyService;

    public SemanticKernelChatService(
        IChatCompletionService chatCompletionService,
#pragma warning disable SKEXP0001
        ITextEmbeddingGenerationService embeddingService,
#pragma warning restore SKEXP0001
        IVectorStore vectorStore,
        IHistoryService historyService)
    {
        _chatCompletionService = chatCompletionService;
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _historyService = historyService;
    }

    public async Task<ChatReply> GetReplyAsync(string message, string sessionId = "default", CancellationToken cancellationToken = default)
    {
        // 1. OBTENER EL VECTOR DE LA PREGUNTA
        var questionEmbedding = await _embeddingService.GenerateEmbeddingAsync(message, cancellationToken: cancellationToken);

        var contextBuilder = new StringBuilder();
        var sources = new HashSet<string>();
        int foundCount = 0;

        try
        {
            var collection = _vectorStore.GetCollection<Guid, DocumentVectorRecord>("document_vectors");
            
            var searchOptions = new VectorSearchOptions
            {
                Top = 7, // Subimos a 7 para PDFs más largos
                Filter = new VectorSearchFilter().EqualTo(nameof(DocumentVectorRecord.SessionId), sessionId)
            };
            
            var searchResults = await collection.VectorizedSearchAsync(questionEmbedding, searchOptions, cancellationToken);

            await foreach (var result in searchResults.Results.WithCancellation(cancellationToken))
            {
                // LOG DE DEPURACIÓN PARA DETECTAR FUGAS
                Console.WriteLine($"[RAG DEBUG] Buscando para Session: {sessionId} | Encontrado en Session: {result.Record.SessionId} | Archivo: {result.Record.FileName} | Score: {result.Score}");

                if (result.Score > 0.35) 
                {
                    contextBuilder.AppendLine($"[Contexto de {result.Record.FileName}]: {result.Record.Text}");
                    contextBuilder.AppendLine("---");
                    sources.Add(result.Record.FileName);
                    foundCount++;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Aviso RAG: Memoria vacía. {ex.Message}");
        }

        var retrievedContext = contextBuilder.ToString();
        Console.WriteLine($"RAG: Se encontraron {foundCount} fragmentos de {sources.Count} archivos.");

        // 3. GESTIÓN DE MEMORIA (HISTORIAL)
        var history = _historyService.GetHistory(sessionId);
        
        // Agregamos la pregunta actual al historial REAL
        _historyService.AddUserMessage(sessionId, message);

        // Creamos la conversación final para el modelo
        var chatToInvoke = new ChatHistory();

        // 1. EL MENSAJE DE SISTEMA CON GUARDRAILS
        var systemMessage = new StringBuilder();
        systemMessage.AppendLine("Eres 'DocMind AI', un asistente de IA corporativo ultra-seguro y preciso.");
        systemMessage.AppendLine("REGLAS DE SEGURIDAD:");
        systemMessage.AppendLine("- Solo responde basándote en el contexto proporcionado.");
        systemMessage.AppendLine("- Si la respuesta no está en el contexto, di: 'Lo siento, esa información no está en los documentos cargados'.");
        systemMessage.AppendLine("- No inventes datos, fechas o nombres que no aparezcan textualmente.");
        systemMessage.AppendLine("- Mantén un tono profesional y evita opiniones personales.");
        systemMessage.AppendLine("- Cita siempre la fuente usando el formato [NombreArchivo.pdf] al final de la frase relevante.");

        if (foundCount > 0)
        {
            systemMessage.AppendLine("\nCONOCIMIENTO CORPORATIVO RELEVANTE:");
            systemMessage.AppendLine(retrievedContext);
        }
        
        chatToInvoke.AddSystemMessage(systemMessage.ToString());

        // 2. AGREGAMOS EL HISTORIAL (Excepto los system prompts antiguos si los hubiera)
        foreach (var msg in history)
        {
            if (msg.Role != AuthorRole.System)
            {
                chatToInvoke.Add(msg);
            }
        }

        // 4. LLAMAR AL MODELO LLM (Ollama)
        Console.WriteLine($"Invocando LLM para sesión '{sessionId}' con {chatToInvoke.Count} mensajes de historial...");
        
        var response = await _chatCompletionService.GetChatMessageContentAsync(chatToInvoke, cancellationToken: cancellationToken);
        
        var reply = response?.Content;
        
        if (string.IsNullOrWhiteSpace(reply))
        {
            Console.WriteLine("ADVERTENCIA: Ollama devolvió una respuesta vacía.");
            reply = "La IA no pudo generar una respuesta en este momento. Inténtalo de nuevo.";
        }

        // Guardamos la respuesta de la IA en el historial REAL para que la recuerde en la próxima pregunta
        _historyService.AddAssistantMessage(sessionId, reply);
        
        return new ChatReply 
        { 
            Response = reply, 
            Sources = sources.ToList() 
        };
    }

    public async IAsyncEnumerable<string> GetStreamingReplyAsync(string message, string sessionId = "default", [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // 1. RAG: Buscar contexto
        var questionEmbedding = await _embeddingService.GenerateEmbeddingAsync(message, cancellationToken: cancellationToken);
        var contextBuilder = new StringBuilder();
        var sources = new HashSet<string>();
        int foundCount = 0;

        var collection = _vectorStore.GetCollection<Guid, DocumentVectorRecord>("document_vectors");
        var searchOptions = new VectorSearchOptions { Top = 7, Filter = new VectorSearchFilter().EqualTo(nameof(DocumentVectorRecord.SessionId), sessionId) };
        
        try {
            var searchResults = await collection.VectorizedSearchAsync(questionEmbedding, searchOptions, cancellationToken);
            await foreach (var result in searchResults.Results.WithCancellation(cancellationToken)) {
                // LOG DE DEPURACIÓN EN STREAMING
                Console.WriteLine($"[RAG STREAM DEBUG] Session actual: {sessionId} | Session fragmento: {result.Record.SessionId} | Score: {result.Score}");

                if (result.Score > 0.35) {
                    contextBuilder.AppendLine($"[Fuente: {result.Record.FileName}]: {result.Record.Text}");
                    contextBuilder.AppendLine("---");
                    sources.Add(result.Record.FileName);
                    foundCount++;
                }
            }
        } catch { /* Ignorar si no hay colección */ }

        // 2. Historial
        var history = _historyService.GetHistory(sessionId);
        _historyService.AddUserMessage(sessionId, message);

        var chatToInvoke = new ChatHistory();
        var systemMessage = new StringBuilder();
        systemMessage.AppendLine("Eres 'DocMind AI', un asistente de IA corporativo seguro.");
        systemMessage.AppendLine("REGLAS: Solo usa el contexto. Si no sabes, di que no está en los documentos. Cita las fuentes usando [NombreArchivo.pdf].");
        
        if (foundCount > 0) 
        {
            systemMessage.AppendLine("\nCONTEXTO EXTRAÍDO:");
            systemMessage.AppendLine(contextBuilder.ToString());
        }
        
        chatToInvoke.AddSystemMessage(systemMessage.ToString());

        foreach (var msg in history) 
        {
            if (msg.Role != AuthorRole.System) chatToInvoke.Add(msg);
        }

        // Agregamos la pregunta actual que no está en el historial cargado todavía
        chatToInvoke.AddUserMessage(message);

        // 3. STREAMING
        var fullReplyBuilder = new StringBuilder();
        Console.WriteLine($"Invocando streaming para sesión '{sessionId}' con {chatToInvoke.Count} mensajes...");
        
        await foreach (var chunk in _chatCompletionService.GetStreamingChatMessageContentsAsync(chatToInvoke, cancellationToken: cancellationToken))
        {
            if (chunk.Content != null)
            {
                fullReplyBuilder.Append(chunk.Content);
                yield return chunk.Content;
            }
        }

        // Guardar la respuesta completa en el historial al finalizar el stream
        _historyService.AddAssistantMessage(sessionId, fullReplyBuilder.ToString());
    }
}
#pragma warning restore SKEXP0026
