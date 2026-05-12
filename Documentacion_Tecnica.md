# 🧠 Documentación Técnica: DocMind AI - RAG System

Este documento detalla la arquitectura y el funcionamiento interno del asistente de IA empresarial desarrollado con .NET 10, Angular 21 y Semantic Kernel.

---

## 🏗️ Arquitectura del Sistema

El proyecto sigue una arquitectura de **N-Capas** (Clean Architecture) para asegurar la escalabilidad:

1.  **API (.NET 10):** Punto de entrada que gestiona los controladores y la comunicación SignalR en tiempo real.
2.  **Application:** Contiene las interfaces y contratos del sistema. Define la lógica de negocio y la gestión de prompts.
3.  **Infrastructure:** La implementación técnica de los servicios:
    *   **Semantic Kernel:** Orquestador principal de la IA.
    *   **Ollama:** Inferencia local de modelos (Llama 3.2 para razonamiento y Nomic-Embed-Text para vectores).
    *   **Qdrant:** Base de datos vectorial persistente para la memoria de largo plazo.

---

## 🔍 El Flujo RAG (Retrieval-Augmented Generation)

El sistema utiliza la técnica RAG para garantizar respuestas basadas únicamente en datos reales de la empresa:

1.  **Ingesta de Documentos (IDP):**
    *   Extracción de texto PDF mediante iTextSharp.
    *   **Chunking Semántico:** Fragmentación en bloques de 1000 caracteres con 200 de solapamiento.
    *   **Vectorización:** Generación de embeddings de 768 dimensiones.
    *   **Indexación:** Almacenamiento en Qdrant vinculando cada fragmento a un `SessionId` único.

2.  **Recuperación y Respuesta:**
    *   Búsqueda de los **7 fragmentos más relevantes** mediante similitud de coseno.
    *   Inyección de contexto en el System Prompt con reglas estrictas de "no alucinación".
    *   Citación automática de fuentes en formato `[NombreArchivo.pdf]`.

---

## 📡 Comunicación y UX Premium

*   **Streaming de Respuesta:** Uso de `IAsyncEnumerable` y **SignalR** para mostrar la respuesta palabra por palabra, mejorando la latencia percibida.
*   **Split-View UI:** Interfaz diseñada en Angular 21 con PrimeNG, permitiendo visualizar el resumen ejecutivo y el chat de forma simultánea.
*   **Aislamiento de Sesiones:** Filtrado estricto por `SessionId` en la base de datos vectorial para garantizar que la información no se filtre entre diferentes conversaciones.

---

## 🛠️ Stack Tecnológico

*   **Backend:** ASP.NET Core 10, Semantic Kernel 1.37.0, Entity Framework Core.
*   **Frontend:** Angular 21, SignalR Client, PrimeNG, CSS Moderno (Glassmorphism).
*   **IA Stack:** Ollama (Llama 3.2), Qdrant Vector Store (Docker).

---

> [!IMPORTANT]
> **Privacidad por Diseño:** Al utilizar modelos locales (Ollama), los datos corporativos nunca salen de la infraestructura del cliente, cumpliendo con normativas de seguridad de datos.

---
*Documentación actualizada el 12 de mayo de 2026 para el Proyecto de Portafolio de Bryan Pino.*

