import { Component, Input } from '@angular/core';
import { NgClass } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-dashboard-card',
  standalone: true,
  imports: [NgClass, RouterLink],
  templateUrl: './dashboard-card.component.html'
})
export class DashboardCardComponent {
  @Input() title: string = '';
  @Input() description: string = '';
  @Input() cardLink: string = '';
  @Input() cardQueryParams: Record<string, string> | null = null;
  @Input() buttonText: string = '';
  @Input() buttonClass: string = 'btn-outline-primary';
}
