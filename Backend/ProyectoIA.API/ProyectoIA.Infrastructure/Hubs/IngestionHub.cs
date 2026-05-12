using Microsoft.AspNetCore.SignalR;

namespace ProyectoIA.Infrastructure.Hubs;

/// <summary>
/// Hub de SignalR para notificar el progreso de la ingesta de documentos en tiempo real.
/// </summary>
public class IngestionHub : Hub
{
    // El cliente se conectará aquí para escuchar los eventos de progreso.
}
