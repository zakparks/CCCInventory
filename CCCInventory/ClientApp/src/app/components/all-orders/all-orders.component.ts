import { Component, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { NavigationExtras, Router } from '@angular/router';
import { OrderService } from '../../services/order.service';
import { Order } from '../../models/order';

@Component({
  selector: 'app-all-orders',
  templateUrl: './all-orders.component.html'
})

export class AllOrdersComponent {
  public orders!: Order[];
  public orderToEdit! : Order;

  constructor(private orderService: OrderService, private router: Router) { }

  ngOnInit(): void {
    this.orderService
      .GetOrders()
      .subscribe(result => {
        this.orders = result;
      });
  }

  initNewOrder() {
    //this.orderToEdit = new Order();
  }

  editOrder(orderToEdit: number) {
    let navDetails: NavigationExtras = {
      queryParams: {
        orderNumber: orderToEdit
      }
    };

    this.router.navigate(["edit-order"], navDetails)
  }
}
