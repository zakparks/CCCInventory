import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { OrderAttachment } from '../models/order-attachment';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AttachmentService {
  private readonly baseUrl = `${environment.apiUrl}/Attachment`;

  constructor(private http: HttpClient) { }

  GetAttachments(orderNumber: number): Observable<OrderAttachment[]> {
    return this.http.get<OrderAttachment[]>(`${this.baseUrl}/${orderNumber}`);
  }

  UploadFiles(orderNumber: number, files: File[]): Observable<OrderAttachment[]> {
    const formData = new FormData();
    files.forEach(f => formData.append('files', f, f.name));
    return this.http.post<OrderAttachment[]>(`${this.baseUrl}/${orderNumber}`, formData);
  }

  DeleteAttachment(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  GetFileUrl(id: number): string {
    return `${this.baseUrl}/${id}/file`;
  }
}
