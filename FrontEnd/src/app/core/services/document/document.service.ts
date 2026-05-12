import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class DocumentService {
  private apiUrl = 'https://localhost:7229/api/Document/upload'; // Ajustado al launchSettings.json

  constructor(private http: HttpClient) { }

  /**
   * Sube un documento PDF al endpoint RAG.
   * @param file El archivo PDF seleccionado.
   * @param connectionId El ID de SignalR para recibir progreso.
   */
  uploadDocument(file: File, connectionId: string, sessionId: string): Observable<any> {
    const formData = new FormData();
    formData.append('file', file, file.name);

    // Enviamos el connectionId para el progreso y el sessionId para el aislamiento de datos
    return this.http.post(`${this.apiUrl}?connectionId=${connectionId}&sessionId=${sessionId}`, formData);
  }

  /**
   * Borra todos los documentos de la memoria de la IA.
   */
  clearDocuments(): Observable<any> {
    const url = this.apiUrl.replace('/upload', '/clear');
    return this.http.delete(url);
  }

  /**
   * Obtiene un resumen generado por IA de un documento específico.
   */
  getDocumentSummary(fileName: string, sessionId: string): Observable<any> {
    const url = this.apiUrl.replace('/upload', '/summary');
    return this.http.get(`${url}?fileName=${fileName}&sessionId=${sessionId}`);
  }
}
