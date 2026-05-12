using Microsoft.OpenApi.Models;
using ProyectoIA.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Registrar nuestros servicios de IA (Ollama + Semantic Kernel)
builder.Services.AddInfrastructureServices();
// Add Swagger/OpenAPI support (Swashbuckle)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ProyectoIA API", Version = "v1" });
});

builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200") // SignalR requiere orígenes específicos si usas credenciales
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Requerido para SignalR
    });
});

var app = builder.Build();

// Crear la base de datos automáticamente si no existe (Solo para Desarrollo)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ProyectoIA.Infrastructure.Persistence.ApplicationDbContext>();
    context.Database.EnsureCreated();
}

// Usar CORS antes del enrutamiento
app.UseCors("AllowAngular");

// Mapear los Hubs de SignalR
app.MapHub<ProyectoIA.Infrastructure.Hubs.IngestionHub>("/ingestionHub");
app.MapHub<ProyectoIA.Infrastructure.Hubs.ChatHub>("/chatHub");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ProyectoIA API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
