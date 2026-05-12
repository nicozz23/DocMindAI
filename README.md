# 🧠 DocMind AI — Intelligent Document Auditor (RAG)

[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Angular 21](https://img.shields.io/badge/Angular-21-DD0031?logo=angular)](https://angular.io/)
[![Semantic Kernel](https://img.shields.io/badge/Semantic_Kernel-Microsoft-blue)](https://learn.microsoft.com/en-us/semantic-kernel/)
[![Ollama](https://img.shields.io/badge/Ollama-Local_LLM-black)](https://ollama.com/)

**DocMind AI** es un sistema avanzado de Auditoría de Documentos basado en **RAG (Retrieval-Augmented Generation)**. Permite a las empresas interactuar con sus documentos internos (PDFs) de forma segura, privada y con respuestas verificables mediante citación de fuentes.

---

## 📺 Demo Video
<div style="position: relative; padding-bottom: 47.39039665970772%; height: 0;"><iframe src="https://www.loom.com/embed/f73b1a11ac5a4d6ea9c54c5e7b21df18" frameborder="0" webkitallowfullscreen mozallowfullscreen allowfullscreen style="position: absolute; top: 0; left: 0; width: 100%; height: 100%;"></iframe></div>

---

## ✨ Funcionalidades Clave

- 📄 **Ingesta de Documentos Pro**: Extracción y fragmentación semántica de PDFs.
- ⚡ **Resumen Ejecutivo Automático**: Generación de resúmenes inmediatos al subir un archivo.
- 🔍 **Búsqueda Semántica**: Recuperación de contexto basada en vectores de alta dimensionalidad.
- 💬 **Chat Interactivo con Fuentes**: Respuestas precisas que indican exactamente de qué archivo y párrafo proviene la información.
- 🔒 **Privacidad Total**: Todo el procesamiento de IA ocurre localmente mediante Ollama; los datos nunca salen de tu infraestructura.

---

## 🏗️ Arquitectura Técnica

El sistema está construido bajo principios de **Clean Architecture** y utiliza:

- **Backend**: ASP.NET Core 10, Semantic Kernel, SignalR (Streaming).
- **Vector Store**: Qdrant (Corriendo en Docker).
- **Modelos**: Llama 3.2 (Inferencia) y Nomic-Embed-Text (Embeddings).
- **Frontend**: Angular 21, PrimeNG, CSS Moderno con efectos de Glassmorphism.

---

## 🚀 Instalación Rápida

### Requisitos
- .NET 10 SDK
- Node.js & Angular CLI
- Docker (Para Qdrant)
- Ollama (Con modelos `llama3.2` y `nomic-embed-text`)

### Pasos
1. **Levantar Qdrant**:
   ```bash
   docker run -p 6333:6333 -p 6334:6334 qdrant/qdrant
   ```
2. **Backend**:
   ```bash
   cd Backend/ProyectoIA.API
   dotnet run
   ```
3. **Frontend**:
   ```bash
   cd FrontEnd
   npm install
   ng serve
   ```

---

## 📄 Documentación
Para más detalles sobre el funcionamiento interno, consulta la [Documentación Técnica](./Docs/Documentacion_Tecnica.md).

---
**Desarrollado por Bryan Pino — AI & Full Stack Engineer**
