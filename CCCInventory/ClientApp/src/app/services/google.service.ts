import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface GoogleStatus {
  configured: boolean;
  connected: boolean;
  accountEmail: string | null;
  templateConfigured: boolean;
}

export interface TemplateCheck {
  templateName: string;
  found: string[];
  unknown: string[];
  unused: string[];
}

export interface ContractResult {
  docId: string;
  url: string;
  name: string;
  generatedAt: string;
  warnings: string[];
}

@Injectable({
  providedIn: 'root'
})
export class GoogleService {
  private readonly baseUrl = `${environment.apiUrl}/Google`;
  private readonly contractUrl = `${environment.apiUrl}/WeddingContract`;

  constructor(private http: HttpClient) { }

  public GetStatus(): Observable<GoogleStatus> {
    return this.http.get<GoogleStatus>(`${this.baseUrl}/status`);
  }

  public GetAuthorizeUrl(): Observable<{ url: string }> {
    const returnOrigin = encodeURIComponent(window.location.origin);
    return this.http.get<{ url: string }>(`${this.baseUrl}/authorize-url?returnOrigin=${returnOrigin}`);
  }

  public Disconnect(): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/disconnect`, {});
  }

  public CheckTemplate(): Observable<TemplateCheck> {
    return this.http.get<TemplateCheck>(`${this.baseUrl}/template-check`);
  }

  // mode is ignored by the server when the order has no contract yet
  public GenerateContract(orderNumber: number, mode: 'overwrite' | 'revision' | null): Observable<ContractResult> {
    return this.http.post<ContractResult>(`${this.contractUrl}/${orderNumber}`, { mode });
  }
}
