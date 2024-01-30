import { Component, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { OrderService } from '../../services/order.service';
import { Order } from '../../models/order';

@Component({
  selector: 'app-all-orders',
  templateUrl: './all-orders.component.html'
})

export class AllOrdersComponent {
  public orders!: Order[];

  constructor(private orderService: OrderService) { }

  ngOnInit(): void {
    this.orderService
      .GetOrders()
      .subscribe(result => {
        this.orders = result;
        console.log("this.orders: ", this.orders);
      });
  }
}
