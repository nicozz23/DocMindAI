import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private hubConnection!: signalR.HubConnection;
  private progressSubject = new BehaviorSubject<number>(0);
  public progress$ = this.progressSubject.asObservable();
  
  public connectionId: string | null = null;

  constructor() {
    this.startConnection();
  }

  private startConnection() {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('https://localhost:7229/ingestionHub') // Ajustado a HTTPS según configuración de Backend
      .withAutomaticReconnect()
      .build();

    this.hubConnection
      .start()
      .then(() => {
        console.log('Conexión con SignalR establecida exitosamente.');
        // Obtenemos el ID único de esta pestaña/sesión para enviarlo al backend
        this.connectionId = this.hubConnection.connectionId;
      })
      .catch(err => console.error('Error al conectar con SignalR: ' + err));

    // Escuchamos el evento de progreso que envía el backend
    this.hubConnection.on('ReceiveProgress', (percentage: number) => {
      this.progressSubject.next(percentage);
    });
  }

  public resetProgress() {
    this.progressSubject.next(0);
  }
}
