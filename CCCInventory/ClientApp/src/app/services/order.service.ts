import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs/internal/Observable';
import { Order } from '../models/order';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class OrderService {
  private url = "Order"

  constructor(private http: HttpClient) { }

  public GetOrders(): Observable<Order[]> {
    return this.http.get<Order[]>(`${environment.apiUrl}/${this.url}`);
  }
}
