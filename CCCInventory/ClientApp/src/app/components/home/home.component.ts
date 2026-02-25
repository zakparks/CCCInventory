import { Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { OrderService } from '../../services/order.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink],
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
