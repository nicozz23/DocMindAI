import { Component, ChangeDetectorRef } from '@angular/core';
import { ChatWindowComponent } from './shared/components/chat-window/chat-window.component';
import { DocumentUploadComponent } from './shared/components/document-upload/document-upload.component';
import { ChatSidebarComponent } from './shared/components/chat-sidebar/chat-sidebar.component';
import { ChatService, ChatSession } from './core/services/chat/chat.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, ChatWindowComponent, DocumentUploadComponent, ChatSidebarComponent],
  templateUrl: './app.html',
  styleUrls: ['./app.css']
})
export class App {
  title = 'DocMind AI';
  sessions: ChatSession[] = [];
  activeSessionId: string = 'default';

  // Split View — resúmenes persistidos por sesión
  showSummaryPanel: boolean = false;
  currentSummary: string = '';
  isLoadingSummary: boolean = false;
  private summaryMap = new Map<string, string>(); // sessionId → resumen

  constructor(
    private chatService: ChatService,
    private cdr: ChangeDetectorRef
  ) {
    this.loadSessions();
  }

  onSummaryGenerated(summary: string) {
    this.summaryMap.set(this.activeSessionId, summary);
    this.currentSummary = summary;
    this.isLoadingSummary = false;
    this.showSummaryPanel = true;
    this.cdr.markForCheck();
  }

  onSummaryLoading(loading: boolean) {
    this.isLoadingSummary = loading;
    if (loading) this.showSummaryPanel = true;
    this.cdr.markForCheck();
  }

  closeSummaryPanel() {
    this.showSummaryPanel = false;
    this.cdr.markForCheck();
  }

  loadSessions() {
    this.chatService.getSessions().subscribe(s => {
      this.sessions = s;
      this.cdr.markForCheck();
    });
  }

  createNewChat() {
    this.activeSessionId = 'chat_' + Math.random().toString(36).substring(2, 9);
    const newSession: ChatSession = {
      id: this.activeSessionId,
      title: 'Nueva Conversación',
      lastUpdate: new Date().toISOString()
    };
    this.sessions = [newSession, ...this.sessions];
    // Nueva sesión no tiene resumen aún
    this.currentSummary = '';
    this.showSummaryPanel = false;
    this.cdr.markForCheck();
  }

  selectSession(id: string) {
    this.activeSessionId = id;
    // Restaurar el resumen de esta sesión si existe
    const saved = this.summaryMap.get(id);
    if (saved) {
      this.currentSummary = saved;
      this.showSummaryPanel = true;
    } else {
      this.currentSummary = '';
      this.showSummaryPanel = false;
    }
    this.cdr.markForCheck();
  }

  deleteSession(id: string) {
    if (!confirm('¿Estás seguro de que quieres eliminar esta conversación?')) return;
    this.chatService.clearHistory(id).subscribe(() => {
      this.summaryMap.delete(id); // limpiar el resumen también
      this.sessions = this.sessions.filter(s => s.id !== id);
      if (this.activeSessionId === id) {
        this.activeSessionId = this.sessions.length > 0 ? this.sessions[0].id : 'default';
        const saved = this.summaryMap.get(this.activeSessionId);
        this.currentSummary = saved ?? '';
        this.showSummaryPanel = !!saved;
      }
      this.cdr.markForCheck();
    });
  }
}
