import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Order } from '../models/order';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class OrderService {
  private readonly baseUrl = `${environment.apiUrl}/Order`;

  constructor(private http: HttpClient) { }

  public GetOrders(): Observable<Order[]> {
    return this.http.get<Order[]>(this.baseUrl);
  }

  public GetOrdersByStatus(status: string): Observable<Order[]> {
    return this.http.get<Order[]>(`${this.baseUrl}?status=${status}`);
  }

  public CancelOrder(orderNumber: number, cancellationReason: string): Observable<number> {
    return this.http.put<number>(`${this.baseUrl}/${orderNumber}/cancel`, { cancellationReason });
  }

  public GetOrder(orderNumber: number): Observable<Order> {
    return this.http.get<Order>(`${this.baseUrl}/${orderNumber}`);
  }

  public AddOrder(order: Order): Observable<number> {
    const { orderNumber, ...orderWithoutOrderNumber } = order;
    return this.http.post<number>(this.baseUrl, orderWithoutOrderNumber);
  }

  public UpdateOrder(order: Order): Observable<number> {
    return this.http.put<number>(this.baseUrl, order);
  }

  public RestoreOrder(orderNumber: number): Observable<number> {
    return this.http.put<number>(`${this.baseUrl}/${orderNumber}/restore`, {});
  }

  public GetCreatedBy(orderNumber: number): Observable<{ staffName: string | null }> {
    return this.http.get<{ staffName: string | null }>(`${this.baseUrl}/${orderNumber}/created-by`);
  }

  public GetNewOrderNumber(): Observable<number> {
    return this.http.get<number>(`${this.baseUrl}/newOrderNumber`);
  }
}
