import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Order } from '../models/order';
import { environment } from '../../environments/environment';

export interface BakeSheetResponse {
  weekStart: string;
  weekEnd: string;
  orders: Order[];
}

@Injectable({
  providedIn: 'root'
})
export class BakeSheetService {
  private readonly baseUrl = `${environment.apiUrl}/BakeSheet`;

  constructor(private http: HttpClient) { }

  getBakeSheet(weekOf?: string): Observable<BakeSheetResponse> {
    const params = weekOf ? `?weekOf=${weekOf}` : '';
    return this.http.get<BakeSheetResponse>(`${this.baseUrl}${params}`);
  }
}
