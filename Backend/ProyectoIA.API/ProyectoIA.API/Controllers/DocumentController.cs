using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProyectoIA.Application.Interfaces;
using System.Threading.Tasks;

namespace ProyectoIA.API.Controllers;

/// <summary>
/// Controlador responsable de recibir archivos desde el exterior (frontend o Swagger).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DocumentController : ControllerBase
{
    private readonly IIngestionService _ingestionService;

    // Inyectamos nuestro servicio de ingesta
    public DocumentController(IIngestionService ingestionService)
    {
        _ingestionService = ingestionService;
    }

    /// <summary>
    /// Sube un archivo PDF, lo lee, lo convierte a vectores y lo guarda en Qdrant.
    /// </summary>
    /// <param name="file">El archivo PDF a procesar.</param>
    /// <param name="connectionId">El identificador de SignalR para progreso.</param>
    /// <param name="sessionId">El identificador del chat para aislamiento.</param>
    /// <returns>Resultado HTTP (200 OK si todo sale bien).</returns>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadDocument(IFormFile file, [FromQuery] string connectionId, [FromQuery] string sessionId)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No se proporcionó ningún archivo.");
        }

        if (file.ContentType != "application/pdf")
        {
            return BadRequest("Actualmente solo se soportan archivos PDF.");
        }

        // Leemos el archivo en memoria y lo pasamos a nuestro servicio
        using var stream = file.OpenReadStream();
        var success = await _ingestionService.IngestPdfAsync(stream, file.FileName, sessionId, connectionId);

        if (success)
        {
            return Ok(new { Message = $"Archivo '{file.FileName}' procesado e integrado a la memoria de la IA exitosamente." });
        }
        else
        {
            return StatusCode(500, "Hubo un error procesando el archivo. Revisa los logs.");
        }
    }

    /// <summary>
    /// Obtiene un resumen generado por IA de un documento específico.
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] string fileName, [FromQuery] string sessionId)
    {
        if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(sessionId))
        {
            return BadRequest("Faltan parámetros: fileName o sessionId.");
        }

        var summary = await _ingestionService.GetDocumentSummaryAsync(fileName, sessionId);
        return Ok(new { Summary = summary });
    }

    /// <summary>
    /// Borra toda la base de datos de conocimientos (vectores) de la IA.
    /// </summary>
    /// <returns>Resultado HTTP (200 OK si se borró todo).</returns>
    [HttpDelete("clear")]
    public async Task<IActionResult> ClearDocuments()
    {
        var success = await _ingestionService.ClearAllDocumentsAsync();
        
        if (success)
        {
            return Ok(new { Message = "Memoria de la IA limpiada correctamente. Ahora la IA no recordará los documentos previos." });
        }

        return StatusCode(500, "Error al intentar borrar la memoria de la IA.");
    }
}
