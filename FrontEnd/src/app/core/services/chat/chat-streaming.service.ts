import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject, Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ChatStreamingService {
  private hubConnection!: signalR.HubConnection;
  private chunkSubject = new Subject<string>();
  private finishedSubject = new Subject<void>();

  public chunk$ = this.chunkSubject.asObservable();
  public finished$ = this.finishedSubject.asObservable();

  constructor() {
    this.startConnection();
  }

  private startConnection() {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('https://localhost:7229/chatHub')
      .withAutomaticReconnect()
      .build();

    this.hubConnection
      .start()
      .then(() => console.log('Conexión con ChatHub establecida.'))
      .catch(err => console.error('Error en ChatHub: ' + err));

    this.hubConnection.on('ReceiveChatChunk', (chunk: string) => {
      this.chunkSubject.next(chunk);
    });

    this.hubConnection.on('ChatStreamFinished', () => {
      this.finishedSubject.next();
    });
  }

  public async sendMessage(message: string, sessionId: string) {
    // Si la conexión está arrancando, esperamos un poco
    if (this.hubConnection.state === signalR.HubConnectionState.Disconnected) {
      try {
        await this.hubConnection.start();
      } catch (err) {
        console.error('No se pudo reconectar:', err);
        return;
      }
    }

    this.hubConnection.invoke('SendMessageStream', message, sessionId)
      .catch(err => console.error('Error al enviar mensaje stream:', err));
  }
}
