import { Component, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { OrderService } from '../../services/order.service';
import { Order } from '../../models/order';

@Component({
  selector: 'app-all-orders',
  templateUrl: './all-orders.component.html'
})

export class AllOrdersComponent {
  public forecasts: WeatherForecast[] = [];
  orders: Order[] = [];

  constructor(private orderService: OrderService) { }

  ngOnInit(): void {
    this.orderService
      .GetOrders()
      .subscribe((result: Order[]) => (this.orders = result));
  }
}

interface WeatherForecast {
  date: string;
  temperatureC: number;
  temperatureF: number;
  summary: string;
}
