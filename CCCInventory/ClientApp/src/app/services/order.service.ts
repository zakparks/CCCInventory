import { Injectable } from '@angular/core';
import { Order } from '../models/order';

@Injectable({
  providedIn: 'root'
})
export class OrderService {

  constructor() { }

  public GetOrders(): Order[] {
    let order = new Order();
    order.OrderNumber = 1;
    order.Details = "cake test";
    order.CustName = "Zak";
    order.CustEmail = "zakparks@gmail.com";

    return [order];
  }
}
