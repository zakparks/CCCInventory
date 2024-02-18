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
  private editUrl = "edit-order"

  constructor(private http: HttpClient) { }

  public GetOrders(): Observable<Order[]> {
    return this.http.get<Order[]>(`${environment.apiUrl}/${this.url}`);
  }

  public GetOrder(orderNumber: number): Observable<Order> {
    var tmp = `${environment.apiUrl}/order/${orderNumber}`;
    console.log("url is: " + tmp);
    return this.http.get<Order>(tmp);
  }

  public AddOrder(order: Order): Observable<Order[]> {
    // Create a copy of the order object without the orderNumber property
    const { orderNumber, ...orderWithoutOrderNumber } = order;
    return this.http.post<Order[]>(`${environment.apiUrl}/${this.url}`, orderWithoutOrderNumber);
  }

  public UpdateOrder(order: Order): Observable<Order[]> {
    return this.http.put<Order[]>(`${environment.apiUrl}/${this.url}`, order);
  }

  public DeleteOrder(orderNumber: number): Observable<Order[]> {
    return this.http.delete<Order[]>(`${environment.apiUrl}/${this.url}/${orderNumber}`);
  }

  public GetNewOrderNumber(): Observable<number> {
    return this.http.get<number>(`${environment.apiUrl}/${this.url}/newOrderNumber`);
  }
}
