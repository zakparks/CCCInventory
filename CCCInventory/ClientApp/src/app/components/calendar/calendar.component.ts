import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Order } from '../../models/order';
import { OrderService } from '../../services/order.service';

interface CalendarDay {
  date: Date;
  isCurrentMonth: boolean;
  isToday: boolean;
  orders: Order[];
}

@Component({
  selector: 'app-calendar',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './calendar.component.html',
  styleUrls: ['./calendar.component.css']
})
export class CalendarComponent implements OnInit {
  viewDate = new Date();
  viewMode: 'month' | 'week' = 'month';
  calendarDays: CalendarDay[] = [];
  allOrders: Order[] = [];

  readonly weekdays = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

  constructor(private orderService: OrderService, private router: Router) { }

  ngOnInit() {
    this.orderService.GetOrders().subscribe(orders => {
      this.allOrders = orders;
      this.build();
    });
  }

  get viewLabel(): string {
    return this.viewMode === 'week' ? this.weekLabel : this.monthLabel;
  }

  get monthLabel(): string {
    return this.viewDate.toLocaleDateString('en-US', { month: 'long', year: 'numeric' });
  }

  get weekLabel(): string {
    const start = this.getWeekStart();
    const end = new Date(start);
    end.setDate(end.getDate() + 6);
    const fmt = (d: Date, opts: Intl.DateTimeFormatOptions) => d.toLocaleDateString('en-US', opts);
    if (start.getFullYear() !== end.getFullYear()) {
      return `${fmt(start, { month: 'short', day: 'numeric', year: 'numeric' })} \u2013 ${fmt(end, { month: 'short', day: 'numeric', year: 'numeric' })}`;
    }
    if (start.getMonth() !== end.getMonth()) {
      return `${fmt(start, { month: 'short', day: 'numeric' })} \u2013 ${fmt(end, { month: 'short', day: 'numeric' })}, ${end.getFullYear()}`;
    }
    return `${fmt(start, { month: 'long', day: 'numeric' })} \u2013 ${end.getDate()}, ${end.getFullYear()}`;
  }

  setViewMode(mode: 'month' | 'week') {
    this.viewMode = mode;
    this.build();
  }

  prevPeriod() {
    if (this.viewMode === 'week') {
      const d = new Date(this.viewDate);
      d.setDate(d.getDate() - 7);
      this.viewDate = d;
    } else {
      this.viewDate = new Date(this.viewDate.getFullYear(), this.viewDate.getMonth() - 1, 1);
    }
    this.build();
  }

  nextPeriod() {
    if (this.viewMode === 'week') {
      const d = new Date(this.viewDate);
      d.setDate(d.getDate() + 7);
      this.viewDate = d;
    } else {
      this.viewDate = new Date(this.viewDate.getFullYear(), this.viewDate.getMonth() + 1, 1);
    }
    this.build();
  }

  goToToday() {
    this.viewDate = new Date();
    this.build();
  }

  editOrder(orderNumber: number) {
    this.router.navigate(['edit-order'], { queryParams: { orderNumber } });
  }

  private build() {
    if (this.viewMode === 'week') {
      this.buildWeek();
    } else {
      this.buildCalendar();
    }
  }

  private getWeekStart(): Date {
    const d = new Date(this.viewDate);
    d.setHours(0, 0, 0, 0);
    d.setDate(d.getDate() - d.getDay());
    return d;
  }

  private buildWeek() {
    const start = this.getWeekStart();
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const days: CalendarDay[] = [];
    for (let i = 0; i < 7; i++) {
      const date = new Date(start);
      date.setDate(date.getDate() + i);
      days.push({
        date,
        isCurrentMonth: true,
        isToday: date.getTime() === today.getTime(),
        orders: this.getOrdersForDate(date)
      });
    }
    this.calendarDays = days;
  }

  private buildCalendar() {
    const year = this.viewDate.getFullYear();
    const month = this.viewDate.getMonth();
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const startDate = new Date(year, month, 1);
    startDate.setDate(startDate.getDate() - startDate.getDay());

    const days: CalendarDay[] = [];
    for (let i = 0; i < 42; i++) {
      const date = new Date(startDate.getFullYear(), startDate.getMonth(), startDate.getDate() + i);
      days.push({
        date,
        isCurrentMonth: date.getMonth() === month,
        isToday: date.getTime() === today.getTime(),
        orders: this.getOrdersForDate(date)
      });
    }
    this.calendarDays = days;
  }

  private getOrdersForDate(date: Date): Order[] {
    return this.allOrders.filter(o => {
      if (!o.orderDateTime) return false;
      const od = new Date(o.orderDateTime);
      return (
        od.getFullYear() === date.getFullYear() &&
        od.getMonth() === date.getMonth() &&
        od.getDate() === date.getDate()
      );
    });
  }
}
