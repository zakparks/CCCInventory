import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavigationExtras, Router, ActivatedRoute } from '@angular/router';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { NgbHighlight } from '@ng-bootstrap/ng-bootstrap';
import { OrderService } from '../../services/order.service';
import { Order } from '../../models/order';

@Component({
  selector: 'app-all-orders',
  standalone: true,
  imports: [ReactiveFormsModule, NgbHighlight, CommonModule],
  templateUrl: './all-orders.component.html'
})
export class AllOrdersComponent implements OnInit {
  allOrders: Order[] = [];
  displayOrders: Order[] = [];

  statusFilter: string = 'active';
  hasIncomplete: boolean = false;

  sortColumn: string = 'orderDateTime';
  sortDir: 'asc' | 'desc' = 'asc';

  filter = new FormControl('', { nonNullable: true });

  constructor(
    private orderService: OrderService,
    private router: Router,
    private route: ActivatedRoute
  ) { }

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      this.statusFilter = params['status'] || 'active';
      this.loadOrders();
    });

    this.loadIncompleteCount();

    this.filter.valueChanges.subscribe(() => this.applyFiltersAndSort());
  }

  loadOrders() {
    this.orderService.GetOrdersByStatus(this.statusFilter).subscribe(orders => {
      this.allOrders = orders;
      this.applyFiltersAndSort();
    });
  }

  loadIncompleteCount() {
    this.orderService.GetOrdersByStatus('incomplete').subscribe(orders => {
      this.hasIncomplete = orders.length > 0;
    });
  }

  setStatus(status: string) {
    this.statusFilter = status;
    if (status === 'incomplete') {
      this.hasIncomplete = false;
    }
    this.loadOrders();
  }

  sort(column: string) {
    if (this.sortColumn === column) {
      this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortColumn = column;
      this.sortDir = 'asc';
    }
    this.applyFiltersAndSort();
  }

  applyFiltersAndSort() {
    const text = this.filter.value.toLowerCase();
    let orders = this.allOrders.filter(o =>
      !text ||
      String(o.orderNumber).includes(text) ||
      (o.custName?.toLowerCase() ?? '').includes(text) ||
      (o.details?.toLowerCase() ?? '').includes(text) ||
      (o.custEmail?.toLowerCase() ?? '').includes(text)
    );

    orders = [...orders].sort((a, b) => {
      const aVal = this.getSortValue(a, this.sortColumn);
      const bVal = this.getSortValue(b, this.sortColumn);
      const cmp = aVal < bVal ? -1 : aVal > bVal ? 1 : 0;
      return this.sortDir === 'asc' ? cmp : -cmp;
    });

    this.displayOrders = orders;
  }

  getSortValue(order: Order, column: string): any {
    switch (column) {
      case 'orderDateTime': return order.orderDateTime ? new Date(order.orderDateTime).getTime() : 0;
      case 'orderNumber': return order.orderNumber ?? 0;
      case 'custName': return order.custName?.toLowerCase() ?? '';
      case 'custEmail': return order.custEmail?.toLowerCase() ?? '';
      case 'details': return order.details?.toLowerCase() ?? '';
      default: return '';
    }
  }

  editOrder(orderNumber: number) {
    const navDetails: NavigationExtras = { queryParams: { orderNumber } };
    this.router.navigate(['edit-order'], navDetails);
  }
}
