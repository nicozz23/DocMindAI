using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using ProyectoIA.Application.Interfaces;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using Microsoft.AspNetCore.SignalR;
using Microsoft.SemanticKernel.ChatCompletion;
using ProyectoIA.Infrastructure.Hubs;

namespace ProyectoIA.Infrastructure.Services;

/// <summary>
/// Servicio responsable de leer documentos, partirlos en trozos y convertirlos en vectores matemáticos.
/// </summary>
public class IngestionService : IIngestionService
{
#pragma warning disable SKEXP0001
    private readonly ITextEmbeddingGenerationService _embeddingService;
#pragma warning restore SKEXP0001
#pragma warning disable SKEXP0026
    private readonly IVectorStore _vectorStore;
    private readonly IHubContext<IngestionHub> _hubContext;
    private readonly IChatCompletionService _chatCompletionService;

    public IngestionService(
#pragma warning disable SKEXP0001
        ITextEmbeddingGenerationService embeddingService, 
#pragma warning restore SKEXP0001
        IVectorStore vectorStore,
        IHubContext<IngestionHub> hubContext,
        IChatCompletionService chatCompletionService)
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _hubContext = hubContext;
        _chatCompletionService = chatCompletionService;
    }

    public async Task<string> GetDocumentSummaryAsync(string fileName, string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = _vectorStore.GetCollection<Guid, DocumentVectorRecord>("document_vectors");
            
            // 1. Buscamos los primeros fragmentos del documento en Qdrant
            // Para el resumen, tomaremos los primeros 3 fragmentos para tener contexto suficiente sin saturar al modelo.
            var searchOptions = new VectorSearchOptions
            {
                Filter = new VectorSearchFilter()
                    .EqualTo(nameof(DocumentVectorRecord.FileName), fileName)
                    .EqualTo(nameof(DocumentVectorRecord.SessionId), sessionId),
                Top = 3
            };

            var results = await collection.VectorizedSearchAsync(new ReadOnlyMemory<float>(new float[768]), searchOptions, cancellationToken);
            var resultList = await results.Results.ToListAsync(cancellationToken);

            if (resultList.Count == 0) return "No se encontró contenido para resumir.";

            var contextBuilder = new StringBuilder();
            foreach (var res in resultList)
            {
                contextBuilder.AppendLine(res.Record.Text);
            }

            // 2. Pedimos a la IA que genere el resumen
            var prompt = $@"Eres un asistente experto en análisis de documentos. 
            A continuación se presentan fragmentos del documento '{fileName}'.
            Por favor, genera un resumen ejecutivo conciso (máximo 3 párrafos) que explique de qué trata el documento y sus puntos clave.
            
            CONTENIDO:
            {contextBuilder}
            
            RESUMEN:";

            var response = await _chatCompletionService.GetChatMessageContentAsync(prompt, cancellationToken: cancellationToken);
            return response.Content ?? "No se pudo generar el resumen.";
        }
        catch (Exception ex)
        {
            return $"Error al generar resumen: {ex.Message}";
        }
    }

    public async Task<bool> ClearAllDocumentsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = _vectorStore.GetCollection<Guid, DocumentVectorRecord>("document_vectors");
            
            // Borramos la colección completa de Qdrant
            await collection.DeleteCollectionAsync(cancellationToken);
            
            Console.WriteLine("Memoria de Qdrant limpiada: Colección 'document_vectors' eliminada.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al limpiar documentos: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> IngestPdfAsync(Stream pdfStream, string fileName, string sessionId, string connectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. EXTRAER EL TEXTO DEL PDF
            // Utilizamos iTextSharp para leer las páginas del PDF y extraer el texto plano.
            // 1. LEER EL PDF
            string fullText = string.Empty;
            var extractedText = new StringBuilder();
            using (PdfReader reader = new PdfReader(pdfStream))
            {
                for (int page = 1; page <= reader.NumberOfPages; page++)
                {
                    var strategy = new SimpleTextExtractionStrategy();
                    var pageText = PdfTextExtractor.GetTextFromPage(reader, page, strategy);
                    extractedText.AppendLine(pageText);
                }
            }

            var text = extractedText.ToString();
            if (string.IsNullOrWhiteSpace(text)) return false;

            // 2. FRAGMENTACIÓN INTELIGENTE (CHUNKING)
            // Usamos un tamaño de 1000 caracteres con 200 de solapamiento (overlap).
            // Esto permite que la IA tenga fragmentos con sentido completo y contexto compartido.
            var chunks = SplitTextIntoChunks(text, 1000, 200);

            // 3. OBTENER LA COLECCIÓN DE QDRANT
            // Prepararemos una "tabla" (colección) en Qdrant llamada "document_vectors".
            var collection = _vectorStore.GetCollection<Guid, DocumentVectorRecord>("document_vectors");
            await collection.CreateCollectionIfNotExistsAsync(cancellationToken);

            // 4. PROCESAR CADA FRAGMENTO
            int totalChunks = chunks.Count;
            int currentChunk = 0;

            foreach (var chunk in chunks)
            {
                currentChunk++;

                // Convertimos el texto en un vector usando Ollama (nomic-embed-text)
                var embedding = await _embeddingService.GenerateEmbeddingAsync(chunk, cancellationToken: cancellationToken);

                // Creamos el registro con sus metadatos
                var record = new DocumentVectorRecord
                {
                    Id = Guid.NewGuid(),
                    SessionId = sessionId, // Vinculamos el documento al CHAT específico
                    FileName = fileName,
                    Text = chunk,
                    Vector = embedding
                };

                // Guardamos en Qdrant
                await collection.UpsertAsync(record, cancellationToken: cancellationToken);

                // Cálculo y envío de progreso en tiempo real via SignalR (usando connectionId)
                if (!string.IsNullOrEmpty(connectionId))
                {
                    int percentage = (int)((float)currentChunk / totalChunks * 100);
                    Console.WriteLine($"Enviando {percentage}% al cliente: {connectionId}");
                    await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveProgress", percentage, cancellationToken);
                }
                else 
                {
                    Console.WriteLine("ADVERTENCIA: connectionId es nulo o vacío, no se envía progreso.");
                }

                if (currentChunk % 5 == 0 || currentChunk == totalChunks)
                {
                    Console.WriteLine($"Progreso: {currentChunk}/{totalChunks} fragmentos procesados.");
                }
                currentChunk++;
            }

            Console.WriteLine("¡Ingesta completada con éxito!");
            return true;
        }
        catch (Exception ex)
        {
            // Loggear el error detallado
            Console.WriteLine($"Error ingestando PDF: {ex.Message}\nDetalles completos:\n{ex}");
            return false;
        }
    }

    /// <summary>
    /// Divide el texto en fragmentos (chunks) usando una ventana deslizante con solapamiento.
    /// Esto mejora la precisión del RAG al mantener el contexto entre bloques.
    /// </summary>
    private List<string> SplitTextIntoChunks(string text, int chunkSize, int overlap = 200)
    {
        var chunks = new List<string>();
        if (string.IsNullOrEmpty(text)) return chunks;

        // Iteramos con un paso menor al tamaño del chunk para crear el solapamiento
        for (int i = 0; i < text.Length; i += (chunkSize - overlap))
        {
            int length = Math.Min(chunkSize, text.Length - i);
            string chunk = text.Substring(i, length);

            // OPTIMIZACIÓN: Intentar no cortar palabras a la mitad buscando el último espacio
            if (i + length < text.Length)
            {
                int lastSpace = chunk.LastIndexOf(' ');
                // Solo ajustamos si el espacio está en un lugar razonable (evita chunks muy pequeños)
                if (lastSpace > chunkSize / 2)
                {
                    length = lastSpace;
                    chunk = chunk.Substring(0, length);
                }
            }

            if (!string.IsNullOrWhiteSpace(chunk))
            {
                chunks.Add(chunk.Trim());
            }

            // Si llegamos al final del texto, salimos del bucle
            if (i + length >= text.Length) break;
        }
        return chunks;
    }
}

/// <summary>
/// Modelo de datos que representa cómo se guarda un fragmento en la base de datos Vectorial.
/// </summary>
#pragma warning disable SKEXP0026
public class DocumentVectorRecord
{
    [VectorStoreRecordKey]
    public Guid Id { get; set; }

    [VectorStoreRecordData(IsFilterable = true)]
    public string SessionId { get; set; } = string.Empty;

    [VectorStoreRecordData(IsFilterable = true)]
    public string FileName { get; set; } = string.Empty;

    [VectorStoreRecordData]
    public string Text { get; set; } = string.Empty;

    // nomic-embed-text genera un vector de 768 dimensiones
    [VectorStoreRecordVector(768)]
    public ReadOnlyMemory<float> Vector { get; set; }
}
#pragma warning restore SKEXP0026
