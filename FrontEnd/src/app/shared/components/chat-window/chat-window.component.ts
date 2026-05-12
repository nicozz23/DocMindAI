import { Component, ElementRef, ViewChild, ChangeDetectorRef, Input, Output, EventEmitter, SimpleChanges, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChatService, ChatResponse } from '../../../core/services/chat/chat.service';
import { ChatStreamingService } from '../../../core/services/chat/chat-streaming.service';
import { Subscription } from 'rxjs';

interface Message {
  text: string;
  isUser: boolean;
  isLoading?: boolean;
  sources?: string[];
}

@Component({
  selector: 'app-chat-window',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chat-window.component.html',
  styleUrls: ['./chat-window.component.css']
})
export class ChatWindowComponent implements OnChanges {
  @Input() sessionId: string = 'default';
  @Output() onMessageSent = new EventEmitter<void>();
  
  messages: Message[] = [
    { text: '¡Hola! Soy tu Asistente Corporativo IA. Pregúntame sobre los documentos de la empresa.', isUser: false }
  ];
  userInput: string = '';
  isWaitingForResponse: boolean = false;

  @ViewChild('scrollContainer') private scrollContainer!: ElementRef;

  private streamingSubscription!: Subscription;

  constructor(
    private chatService: ChatService,
    private chatStreamingService: ChatStreamingService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.streamingSubscription = this.chatStreamingService.chunk$.subscribe(chunk => {
      if (this.isWaitingForResponse) {
        // En el primer chunk, cambiamos el estado de carga a texto real
        const lastMsg = this.messages[this.messages.length - 1];
        if (lastMsg.isLoading) {
          lastMsg.isLoading = false;
          lastMsg.text = chunk;
        } else {
          lastMsg.text += chunk;
        }
        this.scrollToBottom();
        this.cdr.detectChanges();
      }
    });

    this.chatStreamingService.finished$.subscribe(() => {
      // Parsear fuentes del mensaje final
      const lastMsg = this.messages[this.messages.length - 1];
      if (lastMsg && !lastMsg.isUser) {
        const { cleanText, sources } = this.extractSources(lastMsg.text);
        lastMsg.text = cleanText;
        if (sources.length > 0) lastMsg.sources = sources;
      }
      this.isWaitingForResponse = false;
      this.onMessageSent.emit();
      this.cdr.detectChanges();
    });
  }

  ngOnDestroy() {
    if (this.streamingSubscription) {
      this.streamingSubscription.unsubscribe();
    }
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['sessionId'] && !changes['sessionId'].firstChange) {
      this.loadHistory();
    }
  }

  loadHistory() {
    this.isWaitingForResponse = true;
    this.chatService.getHistory(this.sessionId).subscribe({
      next: (history) => {
        this.messages = history.map(m => ({
          text: m.content,
          isUser: m.role.toLowerCase() === 'user',
          sources: m.sources // El historial podría opcionalmente traer fuentes si lo implementáramos en el futuro
        }));
        if (this.messages.length === 0) {
           this.messages = [{ text: '¡Hola! Esta es una nueva conversación.', isUser: false }];
        }
        this.isWaitingForResponse = false;
        this.scrollToBottom();
        this.cdr.markForCheck();
      },
      error: () => {
        this.isWaitingForResponse = false;
        this.cdr.markForCheck();
      }
    });
  }

  sendMessage() {
    if (!this.userInput.trim() || this.isWaitingForResponse) return;

    const messageText = this.userInput;
    this.messages.push({ text: messageText, isUser: true });
    this.userInput = '';
    
    // Iniciar Streaming
    this.isWaitingForResponse = true;
    this.messages.push({ text: '', isUser: false, isLoading: true });
    this.scrollToBottom();

    this.chatStreamingService.sendMessage(messageText, this.sessionId);
  }

  handleKeyPress(event: KeyboardEvent) {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  private scrollToBottom(): void {
    setTimeout(() => {
      try {
        this.scrollContainer.nativeElement.scrollTop = this.scrollContainer.nativeElement.scrollHeight;
      } catch(err) { }
    }, 100);
  }

  /**
   * Extrae fuentes en formato [NombreArchivo.ext] del texto de la IA.
   * Devuelve el texto limpio y un array de fuentes únicas.
   */
  private extractSources(text: string): { cleanText: string; sources: string[] } {
    const pattern = /\[([^\]]+\.(pdf|docx|doc|txt|xlsx|csv))\]/gi;
    const sources = new Set<string>();
    const cleanText = text.replace(pattern, (_, name) => {
      sources.add(name.trim());
      return ''; // eliminar del texto
    }).replace(/\s{2,}/g, ' ').trim();

    return { cleanText, sources: Array.from(sources) };
  }
}
