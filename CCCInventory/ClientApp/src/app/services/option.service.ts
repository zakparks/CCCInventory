import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { OptionItem } from '../models/option-item';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class OptionService {
  private readonly baseUrl = `${environment.apiUrl}/Option`;

  constructor(private http: HttpClient) { }

  getAll(): Observable<OptionItem[]> {
    return this.http.get<OptionItem[]>(this.baseUrl);
  }

  create(item: Partial<OptionItem>): Observable<OptionItem> {
    return this.http.post<OptionItem>(this.baseUrl, item);
  }

  update(id: number, item: Partial<OptionItem>): Observable<OptionItem> {
    return this.http.put<OptionItem>(`${this.baseUrl}/${id}`, item);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  recheck(id: number): Observable<OptionItem> {
    return this.http.post<OptionItem>(`${this.baseUrl}/${id}/recheck`, {});
  }
}
