import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ChatResponse {
  response: string;
  sources: string[];
}

export interface ChatSession {
  id: string;
  title: string;
  lastUpdate: string;
}

export interface ChatHistoryMessage {
  role: string;
  content: string;
  sources?: string[];
}

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  // Asegúrate de que el puerto coincida con el puerto HTTPS/HTTP de tu .NET API
  private apiUrl = 'https://localhost:7229/api/Chat'; // Ajustado al launchSettings.json

  constructor(private http: HttpClient) { }

  sendMessage(message: string, sessionId: string = 'default'): Observable<ChatResponse> {
    return this.http.post<ChatResponse>(this.apiUrl, { message, sessionId });
  }

  getSessions(): Observable<ChatSession[]> {
    return this.http.get<ChatSession[]>(`${this.apiUrl}/sessions`);
  }

  getHistory(sessionId: string): Observable<ChatHistoryMessage[]> {
    return this.http.get<ChatHistoryMessage[]>(`${this.apiUrl}/history/${sessionId}`);
  }

  clearHistory(sessionId: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/history/${sessionId}`);
  }
}
