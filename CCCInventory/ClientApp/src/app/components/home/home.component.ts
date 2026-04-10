import { Component, OnInit } from '@angular/core';
import { OrderService } from '../../services/order.service';
import { DashboardCardComponent } from '../shared/dashboard-card/dashboard-card.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [DashboardCardComponent],
  templateUrl: './home.component.html'
})
export class HomeComponent implements OnInit {
  hasIncompleteOrders: boolean = false;

  constructor(private orderService: OrderService) { }

  ngOnInit() {
    this.orderService.GetOrdersByStatus('incomplete').subscribe(orders => {
      this.hasIncompleteOrders = orders.length > 0;
    });
  }
}
