import { Component } from '@angular/core';
import { CommonModule, AsyncPipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { OrderService } from '../../services/order.service';
import { Order } from '../../models/order';

@Component({
  selector: 'app-deleted-orders',
  standalone: true,
  imports: [AsyncPipe, CommonModule, RouterLink],
  templateUrl: './deleted-orders.component.html'
})
export class DeletedOrdersComponent {
  orders$: Observable<Order[]>;

  constructor(private orderService: OrderService, private router: Router) {
    this.orders$ = this.orderService.GetDeletedOrders();
  }

  restoreOrder(orderNumber: number) {
    this.orderService.RestoreOrder(orderNumber).subscribe(() => {
      this.orders$ = this.orderService.GetDeletedOrders();
    });
  }

  editOrder(orderNumber: number) {
    this.router.navigate(['edit-order'], { queryParams: { orderNumber } });
  }
}
