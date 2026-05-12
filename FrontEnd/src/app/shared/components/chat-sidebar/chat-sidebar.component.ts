import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ChatSession } from '../../../core/services/chat/chat.service';

@Component({
  selector: 'app-chat-sidebar',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="sidebar">
      <button class="new-chat-btn" (click)="onNewChat.emit()">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <path d="M12 5v14M5 12h14"/>
        </svg>
        Nuevo Chat
      </button>

      <div class="sessions-list">
        <div 
          *ngFor="let session of sessions" 
          class="session-item" 
          [class.active]="session.id === activeSessionId"
          (click)="onSelectSession.emit(session.id)"
        >
          <svg class="chat-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/>
          </svg>
          <span class="session-title">{{ session.title }}</span>
          
          <button class="delete-session-btn" (click)="$event.stopPropagation(); onDeleteSession.emit(session.id)">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M3 6h18M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/>
            </svg>
          </button>
        </div>
      </div>

      <div class="sidebar-footer">
         <span class="footer-text">v1.0 Pro Assistant</span>
      </div>
    </div>
  `,
  styles: [`
    .sidebar {
      width: 260px;
      height: 100%;
      background: var(--bg-secondary);
      border-right: 1px solid var(--border-subtle);
      display: flex;
      flex-direction: column;
      padding: 16px;
    }

    .new-chat-btn {
      display: flex;
      align-items: center;
      gap: 10px;
      width: 100%;
      padding: 11px 14px;
      background: transparent;
      border: 1px dashed var(--border-strong);
      border-radius: var(--radius-sm);
      color: var(--text-secondary);
      font-size: 0.85rem;
      font-weight: 500;
      font-family: inherit;
      cursor: pointer;
      transition: all 0.25s var(--ease);
      margin-bottom: 20px;
    }

    .new-chat-btn:hover {
      background: var(--accent-subtle);
      border-color: var(--accent);
      color: var(--text-primary);
    }

    .new-chat-btn svg {
      width: 16px;
      height: 16px;
    }

    .sessions-list {
      flex: 1;
      overflow-y: auto;
      display: flex;
      flex-direction: column;
      gap: 4px;
    }

    .session-item {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 10px 12px;
      border-radius: var(--radius-sm);
      cursor: pointer;
      transition: all 0.2s var(--ease);
      color: var(--text-tertiary);
      border: 1px solid transparent;
    }

    .session-item:hover {
      background: rgba(255, 255, 255, 0.03);
      color: var(--text-secondary);
    }

    .session-item.active {
      background: var(--accent-subtle);
      color: var(--text-primary);
      border-color: rgba(139, 92, 246, 0.2);
    }

    .chat-icon {
      width: 15px;
      height: 15px;
      flex-shrink: 0;
      opacity: 0.6;
    }

    .session-item.active .chat-icon {
      opacity: 1;
      color: var(--accent);
    }

    .session-title {
      font-size: 0.82rem;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      flex: 1;
    }

    .delete-session-btn {
      background: transparent;
      border: none;
      color: var(--text-tertiary);
      padding: 4px;
      cursor: pointer;
      opacity: 0;
      transition: all 0.2s var(--ease);
      display: flex;
      align-items: center;
      border-radius: 4px;
    }

    .session-item:hover .delete-session-btn {
      opacity: 1;
    }

    .delete-session-btn:hover {
      color: var(--error);
      background: rgba(239, 68, 68, 0.1);
    }

    .delete-session-btn svg {
      width: 13px;
      height: 13px;
    }

    .sidebar-footer {
      padding-top: 12px;
      border-top: 1px solid var(--border-subtle);
      text-align: center;
    }

    .footer-text {
      font-size: 0.65rem;
      color: var(--text-tertiary);
      text-transform: uppercase;
      letter-spacing: 1.5px;
    }
  `]
})
export class ChatSidebarComponent {
  @Input() sessions: ChatSession[] = [];
  @Input() activeSessionId: string = '';
  @Output() onNewChat = new EventEmitter<void>();
  @Output() onSelectSession = new EventEmitter<string>();
  @Output() onDeleteSession = new EventEmitter<string>();
}
