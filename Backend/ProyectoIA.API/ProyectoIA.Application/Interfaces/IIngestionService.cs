using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ProyectoIA.Application.Interfaces;

/// <summary>
/// Contrato que define las operaciones para la "ingesta" o lectura de documentos.
/// Esto permite que la API no dependa de cómo se extrae el texto ni de cómo se guarda.
/// </summary>
public interface IIngestionService
{
    /// <summary>
    /// Procesa un archivo PDF, extrae su texto, lo convierte a vectores y lo guarda en la base de datos.
    /// </summary>
    /// <param name="fileStream">Flujo de datos del archivo subido.</param>
    /// <param name="fileName">Nombre original del archivo para tener referencia.</param>
    /// <param name="sessionId">El identificador de la conversación (para aislamiento).</param>
    /// <param name="connectionId">El identificador de SignalR (para progreso).</param>
    /// <returns>Verdadero si el proceso fue exitoso.</returns>
    Task<bool> IngestPdfAsync(Stream pdfStream, string fileName, string sessionId, string connectionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Borra todos los documentos y vectores almacenados en el sistema.
    /// </summary>
    Task<bool> ClearAllDocumentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Genera un resumen ejecutivo de un documento específico.
    /// </summary>
    /// <param name="fileName">Nombre del archivo a resumir.</param>
    /// <param name="sessionId">ID de la sesión.</param>
    Task<string> GetDocumentSummaryAsync(string fileName, string sessionId, CancellationToken cancellationToken = default);
}
