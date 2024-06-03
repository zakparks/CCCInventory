import { Component, PipeTransform } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { NavigationExtras, Router } from '@angular/router';
import { AsyncPipe, DecimalPipe } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { NgbHighlight } from '@ng-bootstrap/ng-bootstrap';
import { Observable, of } from 'rxjs';
import { map, shareReplay, switchMap, startWith } from 'rxjs/operators';
import { OrderService } from '../../services/order.service';
import { Order } from '../../models/order';

@Component({
  selector: 'app-all-orders',
  standalone: true,
  imports: [DecimalPipe, AsyncPipe, ReactiveFormsModule, NgbHighlight, CommonModule],
  templateUrl: './all-orders.component.html',
  providers: [DecimalPipe]
})

export class AllOrdersComponent {
  public orderToEdit!: Order;
  orders$: Observable<Order[]>;
  filteredOrders$: Observable<Order[]>;
  filter = new FormControl('', { nonNullable: true });

  constructor(private orderService: OrderService, private router: Router, pipe: DecimalPipe) {
    this.orders$ = this.orderService.GetOrders().pipe(
      shareReplay(1),
    );

    this.filteredOrders$ = this.filter.valueChanges.pipe(
      startWith(''),
      switchMap(text => this.orders$.pipe(
        map(orders => this.search(text, orders, pipe))
      ))
    );
  }

  search(text: string, orders: Order[], pipe: PipeTransform): Order[] {
    const searchTerm = text.toLowerCase();
    return orders.filter(order => (
      pipe.transform(order.orderNumber).includes(searchTerm) ||
      order.details!.toLowerCase().includes(searchTerm) ||
      order.custEmail!.toLowerCase().includes(searchTerm)
    ));
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
