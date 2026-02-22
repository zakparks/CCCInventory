import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { SignatureCupcake } from '../models/signature-cupcake';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class SignatureCupcakeService {
  private readonly baseUrl = `${environment.apiUrl}/SignatureCupcake`;

  constructor(private http: HttpClient) { }

  getAll(): Observable<SignatureCupcake[]> {
    return this.http.get<SignatureCupcake[]>(this.baseUrl);
  }

  create(item: Partial<SignatureCupcake>): Observable<SignatureCupcake> {
    return this.http.post<SignatureCupcake>(this.baseUrl, item);
  }

  update(id: number, item: Partial<SignatureCupcake>): Observable<SignatureCupcake> {
    return this.http.put<SignatureCupcake>(`${this.baseUrl}/${id}`, item);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
