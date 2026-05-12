using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using ProyectoIA.Application.Interfaces;
using ProyectoIA.Infrastructure.Services;
using ProyectoIA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ProyectoIA.Infrastructure.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // 0. Configurar SQL Server
        var connectionString = "Server=.\\SQLEXPRESS;Database=CerebroCorpDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
        
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        // 1. Configurar Semantic Kernel
        var builder = Kernel.CreateBuilder();
        
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0070
        builder.AddOllamaChatCompletion(
            modelId: "llama3.2", 
            endpoint: new Uri("http://localhost:11434")
        );

        builder.AddOllamaTextEmbeddingGeneration(
            modelId: "nomic-embed-text",
            endpoint: new Uri("http://localhost:11434")
        );
#pragma warning restore SKEXP0070
#pragma warning restore SKEXP0001

        var kernel = builder.Build();

        // 2. Servicios de IA
        services.AddSingleton(kernel); // Registrar el Kernel completo para que los servicios puedan usarlo
        services.AddSingleton(kernel.GetRequiredService<IChatCompletionService>());
#pragma warning disable SKEXP0001
        services.AddSingleton(kernel.GetRequiredService<ITextEmbeddingGenerationService>());
#pragma warning restore SKEXP0001
        
        // 3. Vector Store
#pragma warning disable SKEXP0026
        services.AddQdrantVectorStore("localhost", 6334, https: false);
#pragma warning restore SKEXP0026

        // 4. Servicios de Aplicación
        services.AddScoped<IHistoryService, SqlHistoryService>();
        services.AddScoped<IChatService, SemanticKernelChatService>();
        services.AddScoped<IIngestionService, IngestionService>();

        return services;
    }
}
