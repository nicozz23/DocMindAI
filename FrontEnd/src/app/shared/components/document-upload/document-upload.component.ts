import { ChangeDetectorRef, Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DocumentService } from '../../../core/services/document/document.service';
import { SignalRService } from '../../../core/services/signalr/signalr.service';

@Component({
  selector: 'app-document-upload',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './document-upload.component.html',
  styleUrls: ['./document-upload.component.css']
})
export class DocumentUploadComponent {
  @Input() sessionId: string = 'default';
  @Output() onSummaryLoading = new EventEmitter<boolean>();
  @Output() onSummaryGenerated = new EventEmitter<string>();

  isDragging = false;
  selectedFile: File | null = null;
  isUploading = false;
  uploadStatus: 'idle' | 'success' | 'error' = 'idle';
  statusMessage = '';
  progress = 0;

  constructor(
    private documentService: DocumentService,
    public signalRService: SignalRService,
    private cdr: ChangeDetectorRef
  ) {
    // Escuchar el progreso real desde SignalR
    this.signalRService.progress$.subscribe(p => {
      this.progress = p;
      this.cdr.markForCheck(); 
    });
  }

  onDragOver(event: DragEvent) {
    event.preventDefault();
    this.isDragging = true;
  }

  onDragLeave(event: DragEvent) {
    event.preventDefault();
    this.isDragging = false;
  }

  onDrop(event: DragEvent) {
    event.preventDefault();
    this.isDragging = false;

    if (event.dataTransfer?.files && event.dataTransfer.files.length > 0) {
      this.handleFile(event.dataTransfer.files[0]);
    }
  }

  onFileSelected(event: any) {
    if (event.target.files && event.target.files.length > 0) {
      this.handleFile(event.target.files[0]);
    }
  }

  private handleFile(file: File) {
    if (file.type !== 'application/pdf') {
      this.uploadStatus = 'error';
      this.statusMessage = 'Solo se permiten archivos PDF.';
      return;
    }

    this.selectedFile = file;
    this.uploadStatus = 'idle';
    this.statusMessage = '';
    this.signalRService.resetProgress(); 
    this.cdr.markForCheck();
  }

  uploadFile() {
    if (!this.selectedFile) return;

    const fileName = this.selectedFile.name;
    this.isUploading = true;
    this.uploadStatus = 'idle';
    this.signalRService.resetProgress();

    const connectionId = this.signalRService.connectionId ?? '';

    this.documentService.uploadDocument(this.selectedFile, connectionId, this.sessionId).subscribe({
      next: (res) => {
        this.isUploading = false;
        this.uploadStatus = 'success';
        this.statusMessage = 'Documento integrado.';
        this.selectedFile = null;
        this.progress = 100;
        
        // ¡NUEVO!: Pedir el resumen automáticamente
        this.getSummary(fileName);
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.isUploading = false;
        this.uploadStatus = 'error';
        this.statusMessage = 'Hubo un error al subir el documento.';
        console.error(err);
      }
    });
  }

  private getSummary(fileName: string) {
    this.onSummaryLoading.emit(true);
    this.cdr.markForCheck();

    this.documentService.getDocumentSummary(fileName, this.sessionId).subscribe({
      next: (res) => {
        this.onSummaryGenerated.emit(res.summary);
        this.cdr.markForCheck();
      },
      error: () => {
        this.onSummaryLoading.emit(false);
        this.cdr.markForCheck();
      }
    });
  }

  clearMemory() {
    if (!confirm('¿Estás seguro de que quieres borrar toda la memoria de la IA?')) return;

    this.isUploading = true; // Reutilizamos el estado para deshabilitar botones
    this.documentService.clearDocuments().subscribe({
      next: (res) => {
        this.isUploading = false;
        this.uploadStatus = 'success';
        this.statusMessage = 'Memoria de la IA limpiada correctamente.';
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.isUploading = false;
        this.uploadStatus = 'error';
        this.statusMessage = 'Error al intentar limpiar la memoria.';
        console.error(err);
        this.cdr.markForCheck();
      }
    });
  }
}
